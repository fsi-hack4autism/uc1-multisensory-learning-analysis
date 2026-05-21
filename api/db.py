from __future__ import annotations

import json
import os
import uuid
from datetime import datetime
from typing import Any, Optional

from sqlalchemy.ext.asyncio import AsyncSession, create_async_engine
from sqlalchemy.orm import sessionmaker

from models import (
    AnalysisResponse,
    Base,
    LearningSession,
    SessionMetric,
    SessionRecommendation,
    VisualSignalDB,
)

_engine = None
_AsyncSessionLocal = None


def is_enabled() -> bool:
    return bool(os.environ.get("DATABASE_URL"))


async def init_db() -> None:
    global _engine, _AsyncSessionLocal
    db_url = os.environ.get("DATABASE_URL")
    if not db_url:
        return
    _engine = create_async_engine(db_url, echo=False, future=True)
    _AsyncSessionLocal = sessionmaker(_engine, class_=AsyncSession, expire_on_commit=False)
    async with _engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)


async def save_session(result: AnalysisResponse, rubric: Optional[dict[str, Any]] = None) -> None:
    if _AsyncSessionLocal is None:
        return

    session_uuid = uuid.UUID(result.session_id)

    db_session = LearningSession(
        id=session_uuid,
        title=result.filename,
        source_file_name=result.filename,
        media_type="video/mp4" if result.visual_signals is not None else "audio/mpeg",
        status="completed",
        transcript=result.transcript,
        overall_session_notes=result.overall_session_notes,
        created_at_utc=result.analyzed_at,
        processed_at_utc=datetime.utcnow(),
        rubric_json=json.dumps(rubric) if rubric else None,
    )

    _confidence_float = {"high": 0.9, "medium": 0.6, "low": 0.3}

    metrics = [
        SessionMetric(
            id=uuid.uuid4(),
            learning_session_id=session_uuid,
            metric_key="emotion_overwhelm",
            metric_label="Emotion / Overwhelm",
            score=result.emotion_overwhelm.score,
            rating=result.emotion_overwhelm.confidence,
            explanation=result.emotion_overwhelm.summary,
            confidence=_confidence_float[result.emotion_overwhelm.confidence],
            detail_json=json.dumps({
                "detected": result.emotion_overwhelm.detected,
                "signals": result.emotion_overwhelm.signals,
                "timestamps": [
                    {"start": t.start, "end": t.end, "description": t.description}
                    for t in result.emotion_overwhelm.timestamps
                ],
            }),
        ),
        SessionMetric(
            id=uuid.uuid4(),
            learning_session_id=session_uuid,
            metric_key="echolalia_scripting",
            metric_label="Echolalia / Scripting",
            score=result.echolalia_scripting.score,
            rating=result.echolalia_scripting.confidence,
            explanation=result.echolalia_scripting.summary,
            confidence=_confidence_float[result.echolalia_scripting.confidence],
            detail_json=json.dumps({
                "detected": result.echolalia_scripting.detected,
                "echolalia_type": result.echolalia_scripting.echolalia_type,
                "instances": [
                    {"phrase": i.phrase, "repetition_count": i.repetition_count, "timestamp": i.timestamp}
                    for i in result.echolalia_scripting.instances
                ],
            }),
        ),
        SessionMetric(
            id=uuid.uuid4(),
            learning_session_id=session_uuid,
            metric_key="conversational_context",
            metric_label="Conversational Context",
            score=result.conversational_context.score,
            rating=result.conversational_context.confidence,
            explanation=result.conversational_context.summary,
            confidence=_confidence_float[result.conversational_context.confidence],
            detail_json=json.dumps({
                "following_context": result.conversational_context.following_context,
                "context_breaks": [
                    {"timestamp": cb.timestamp, "description": cb.description}
                    for cb in result.conversational_context.context_breaks
                ],
            }),
        ),
    ]

    recommendations = [
        SessionRecommendation(
            id=uuid.uuid4(),
            learning_session_id=session_uuid,
            recommendation_text=text,
            priority=i + 1,
        )
        for i, text in enumerate(result.recommendations)
    ]

    visual_signals_db = []
    if result.visual_signals:
        visual_signals_db = [
            VisualSignalDB(
                id=uuid.uuid4(),
                learning_session_id=session_uuid,
                timestamp=vs.timestamp,
                signal_type=vs.signal_type,
                label=vs.label,
                confidence=vs.confidence,
                explanation=vs.explanation,
            )
            for vs in result.visual_signals
        ]

    async with _AsyncSessionLocal() as sess:
        async with sess.begin():
            sess.add(db_session)
            sess.add_all(metrics)
            sess.add_all(recommendations)
            if visual_signals_db:
                sess.add_all(visual_signals_db)
