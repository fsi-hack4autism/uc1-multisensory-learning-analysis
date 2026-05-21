from __future__ import annotations

import json
import uuid
from contextlib import asynccontextmanager
from datetime import datetime
from typing import Any, Optional

from dotenv import load_dotenv
from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from fastapi.responses import JSONResponse

import db
from analyzer import DEFAULT_SCORING_RUBRIC, GeminiABAAnalyzer, _detect_mime
from models import AnalysisResponse

load_dotenv()

MAX_UPLOAD_BYTES = 20 * 1024 * 1024  # 20 MB
ALLOWED_MIME_TYPES = {"audio/mpeg", "video/mp4"}


@asynccontextmanager
async def lifespan(app: FastAPI):
    await db.init_db()
    yield


app = FastAPI(
    title="ABA Session Analyzer",
    description="Analyzes ABA therapy session audio/video using Gemini 3.5 Flash via Vertex AI.",
    version="1.0.0",
    lifespan=lifespan,
)

analyzer = GeminiABAAnalyzer()


@app.get("/health")
async def health():
    return {"status": "ok", "db_enabled": db.is_enabled()}


@app.get(
    "/rubric",
    summary="Get the default scoring rubric",
    description=(
        "Returns the default scoring rubric used by /analyze. "
        "Pass a JSON-serialized subset of this structure as `rubric_overrides` "
        "to /analyze to customize thresholds for a specific session."
    ),
)
async def get_rubric():
    return DEFAULT_SCORING_RUBRIC


@app.post("/analyze", response_model=AnalysisResponse)
async def analyze_session(
    audio: UploadFile = File(..., description="MP3 or MP4 file, max 20 MB"),
    context: Optional[str] = Form(default=None, description="Optional therapist context notes"),
    rubric_overrides: Optional[str] = Form(
        default=None,
        description=(
            "Optional JSON object to override specific scoring rubric sections. "
            "Top-level keys (emotion_overwhelm, echolalia_scripting, conversational_context) "
            "replace the corresponding default rubric section entirely. "
            "Fetch GET /rubric to see the full default structure."
        ),
    ),
):
    media_bytes = await audio.read()

    if len(media_bytes) > MAX_UPLOAD_BYTES:
        raise HTTPException(status_code=413, detail="File exceeds 20 MB limit.")

    mime_type = _detect_mime(media_bytes, audio.content_type)

    if mime_type not in ALLOWED_MIME_TYPES:
        raise HTTPException(
            status_code=415,
            detail=f"Unsupported media type '{audio.content_type}'. Use audio/mpeg (MP3) or video/mp4.",
        )

    overrides: dict[str, Any] = {}
    if rubric_overrides:
        try:
            overrides = json.loads(rubric_overrides)
            if not isinstance(overrides, dict):
                raise ValueError("rubric_overrides must be a JSON object")
        except (json.JSONDecodeError, ValueError) as exc:
            raise HTTPException(status_code=422, detail=f"Invalid rubric_overrides JSON: {exc}") from exc

    active_rubric = {**DEFAULT_SCORING_RUBRIC, **overrides}

    session_id = str(uuid.uuid4())
    analyzed_at = datetime.utcnow()

    try:
        raw = analyzer.analyze(
            media_bytes=media_bytes,
            filename=audio.filename or "upload",
            mime_type=mime_type,
            context=context,
            rubric=active_rubric,
        )
    except Exception as exc:
        raise HTTPException(status_code=502, detail=f"Gemini analysis failed: {exc}") from exc

    try:
        result = analyzer.build_response(
            raw=raw,
            session_id=session_id,
            filename=audio.filename or "upload",
            analyzed_at=analyzed_at,
            is_video=(mime_type == "video/mp4"),
        )
    except Exception as exc:
        raise HTTPException(status_code=500, detail=f"Response mapping failed: {exc}") from exc

    if db.is_enabled():
        try:
            await db.save_session(result, rubric=active_rubric)
        except Exception:
            pass  # DB persistence is best-effort; don't fail the request

    return result
