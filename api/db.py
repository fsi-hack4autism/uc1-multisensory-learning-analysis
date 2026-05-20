from __future__ import annotations

import os
import uuid
from datetime import datetime
from typing import Optional

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


async def save_session(result: AnalysisResponse) -> None:
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
    )

    metrics = [
        SessionMetric(
            id=uuid.uuid4(),
            learning_session_id=session_uuid,
            metric_key="emotion_overwhelm",
            metric_label="Emotion / Overwhelm",
            score=result.emotion_overwhelm.score,
            rating=result.emotion_overwhelm.confidence,
            explanation=result.emotion_overwhelm.summary,
            confidence={"high": 0.9, "medium": 0.6, "low": 0.3}[result.emotion_overwhelm.confidence],
        ),
        SessionMetric(
            id=uuid.uuid4(),
            learning_session_id=session_uuid,
            metric_key="echolalia_scripting",
            metric_label="Echolalia / Scripting",
            score=result.echolalia_scripting.score,
            rating=result.echolalia_scripting.confidence,
            explanation=result.echolalia_scripting.summary,
            confidence={"high": 0.9, "medium": 0.6, "low": 0.3}[result.echolalia_scripting.confidence],
        ),
        SessionMetric(
            id=uuid.uuid4(),
            learning_session_id=session_uuid,
            metric_key="conversational_context",
            metric_label="Conversational Context",
            score=result.conversational_context.score,
            rating=result.conversational_context.confidence,
            explanation=result.conversational_context.summary,
            confidence={"high": 0.9, "medium": 0.6, "low": 0.3}[result.conversational_context.confidence],
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
