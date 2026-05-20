from __future__ import annotations

import uuid
from contextlib import asynccontextmanager
from datetime import datetime
from typing import Optional

from dotenv import load_dotenv
from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from fastapi.responses import JSONResponse

import db
from analyzer import GeminiABAAnalyzer, _detect_mime
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
    description="Analyzes ABA therapy session audio/video using Gemini 2.5 Flash via Vertex AI.",
    version="1.0.0",
    lifespan=lifespan,
)

analyzer = GeminiABAAnalyzer()


@app.get("/health")
async def health():
    return {"status": "ok", "db_enabled": db.is_enabled()}


@app.post("/analyze", response_model=AnalysisResponse)
async def analyze_session(
    audio: UploadFile = File(..., description="MP3 or MP4 file, max 20 MB"),
    context: Optional[str] = Form(default=None, description="Optional therapist context notes"),
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

    session_id = str(uuid.uuid4())
    analyzed_at = datetime.utcnow()

    try:
        raw = analyzer.analyze(
            media_bytes=media_bytes,
            filename=audio.filename or "upload",
            mime_type=mime_type,
            context=context,
        )
    except Exception as exc:
        raise HTTPException(status_code=502, detail=f"Gemini analysis failed: {exc}") from exc

    result = analyzer.build_response(
        raw=raw,
        session_id=session_id,
        filename=audio.filename or "upload",
        analyzed_at=analyzed_at,
        is_video=(mime_type == "video/mp4"),
    )

    if db.is_enabled():
        try:
            await db.save_session(result)
        except Exception:
            pass  # DB persistence is best-effort; don't fail the request

    return result
