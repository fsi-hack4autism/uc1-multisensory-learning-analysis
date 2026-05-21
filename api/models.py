from __future__ import annotations

import uuid
from datetime import datetime
from typing import Literal

from pydantic import BaseModel, Field
# Uuid (not sqlalchemy.dialects.postgresql.UUID) is intentional: it maps to
# native UUID on Postgres and String(32) on SQLite, so switching backends is
# a one-line DATABASE_URL change in .env — no schema migration needed.
from sqlalchemy import Column, DateTime, Float, ForeignKey, Integer, String, Text, Uuid
from sqlalchemy.orm import DeclarativeBase, relationship


# ---------------------------------------------------------------------------
# Pydantic — API response models
# ---------------------------------------------------------------------------

class SignalTimestamp(BaseModel):
    start: str
    end: str
    description: str


class EmotionOverwhelmResult(BaseModel):
    detected: bool
    confidence: Literal["high", "medium", "low"]
    score: float = Field(ge=0.0, le=1.0)
    signals: list[str]
    timestamps: list[SignalTimestamp]
    summary: str


class EcholaliaInstance(BaseModel):
    phrase: str
    repetition_count: int
    timestamp: str


class EcholaliaScriptingResult(BaseModel):
    detected: bool
    confidence: Literal["high", "medium", "low"]
    score: float = Field(ge=0.0, le=1.0)
    echolalia_type: Literal["immediate", "delayed", "scripted", "none"]
    instances: list[EcholaliaInstance]
    summary: str


class ContextBreak(BaseModel):
    timestamp: str
    description: str


class ConversationalContextResult(BaseModel):
    following_context: bool
    confidence: Literal["high", "medium", "low"]
    score: float = Field(ge=0.0, le=1.0)
    context_breaks: list[ContextBreak]
    summary: str


class VisualSignal(BaseModel):
    timestamp: str
    signal_type: str
    label: str
    confidence: Literal["high", "medium", "low"]
    explanation: str


class AnalysisResponse(BaseModel):
    session_id: str
    filename: str
    analyzed_at: datetime
    emotion_overwhelm: EmotionOverwhelmResult
    echolalia_scripting: EcholaliaScriptingResult
    conversational_context: ConversationalContextResult
    transcript: str
    overall_session_notes: str
    recommendations: list[str]
    visual_signals: list[VisualSignal] | None = None


# ---------------------------------------------------------------------------
# Pydantic — Gemini structured-output schema (no Literal, no Optional)
# Used only as response_schema for the Vertex AI call.
# ---------------------------------------------------------------------------

class _GeminiSignalTimestamp(BaseModel):
    start: str
    end: str
    description: str


class _GeminiEmotionOverwhelmResult(BaseModel):
    detected: bool
    confidence: str
    score: float
    signals: list[str]
    timestamps: list[_GeminiSignalTimestamp]
    summary: str


class _GeminiEcholaliaInstance(BaseModel):
    phrase: str
    repetition_count: int
    timestamp: str


class _GeminiEcholaliaScriptingResult(BaseModel):
    detected: bool
    confidence: str
    score: float
    echolalia_type: str
    instances: list[_GeminiEcholaliaInstance]
    summary: str


class _GeminiContextBreak(BaseModel):
    timestamp: str
    description: str


class _GeminiConversationalContextResult(BaseModel):
    following_context: bool
    confidence: str
    score: float
    context_breaks: list[_GeminiContextBreak]
    summary: str


class _GeminiVisualSignal(BaseModel):
    timestamp: str
    signal_type: str
    label: str
    confidence: str
    explanation: str


class GeminiAnalysisSchema(BaseModel):
    emotion_overwhelm: _GeminiEmotionOverwhelmResult
    echolalia_scripting: _GeminiEcholaliaScriptingResult
    conversational_context: _GeminiConversationalContextResult
    transcript: str
    overall_session_notes: str
    recommendations: list[str]
    visual_signals: list[_GeminiVisualSignal]


# ---------------------------------------------------------------------------
# SQLAlchemy — DB models (learning_sessions + session_metrics)
# ---------------------------------------------------------------------------

class Base(DeclarativeBase):
    pass


class LearningSession(Base):
    __tablename__ = "learning_sessions"

    id = Column(Uuid(as_uuid=True), primary_key=True, default=uuid.uuid4)
    title = Column(String(255))
    source_file_name = Column(String(255))
    media_type = Column(String(64))
    status = Column(String(32), default="completed")
    transcript = Column(Text)
    overall_session_notes = Column(Text)
    created_at_utc = Column(DateTime, default=datetime.utcnow)
    processed_at_utc = Column(DateTime)
    # Scoring rubric (default or therapist-customized) active for this session
    rubric_json = Column(Text)

    metrics = relationship("SessionMetric", back_populates="session", cascade="all, delete-orphan")
    recommendations = relationship("SessionRecommendation", back_populates="session", cascade="all, delete-orphan")
    visual_signals = relationship("VisualSignalDB", back_populates="session", cascade="all, delete-orphan")


class SessionMetric(Base):
    __tablename__ = "session_metrics"

    id = Column(Uuid(as_uuid=True), primary_key=True, default=uuid.uuid4)
    learning_session_id = Column(Uuid(as_uuid=True), ForeignKey("learning_sessions.id"), nullable=False)
    metric_key = Column(String(128), nullable=False)
    metric_label = Column(String(255))
    score = Column(Float)
    rating = Column(String(32))
    explanation = Column(Text)
    confidence = Column(Float)
    # Full per-metric breakdown: timestamps/instances/context_breaks serialized as JSON
    detail_json = Column(Text)

    session = relationship("LearningSession", back_populates="metrics")


class SessionRecommendation(Base):
    __tablename__ = "session_recommendations"

    id = Column(Uuid(as_uuid=True), primary_key=True, default=uuid.uuid4)
    learning_session_id = Column(Uuid(as_uuid=True), ForeignKey("learning_sessions.id"), nullable=False)
    recommendation_text = Column(Text)
    priority = Column(Integer)

    session = relationship("LearningSession", back_populates="recommendations")


class VisualSignalDB(Base):
    __tablename__ = "visual_signals"

    id = Column(Uuid(as_uuid=True), primary_key=True, default=uuid.uuid4)
    learning_session_id = Column(Uuid(as_uuid=True), ForeignKey("learning_sessions.id"), nullable=False)
    timestamp = Column(String(32))
    signal_type = Column(String(64))
    label = Column(String(255))
    confidence = Column(String(32))
    explanation = Column(Text)

    session = relationship("LearningSession", back_populates="visual_signals")
