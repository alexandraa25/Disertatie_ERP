from fastapi import FastAPI
from pydantic import BaseModel
from transformers import pipeline
from keybert import KeyBERT
from typing import List, Optional, Literal
import re

app = FastAPI()

sentiment_model = pipeline(
    "sentiment-analysis",
    model="nlptown/bert-base-multilingual-uncased-sentiment"
)

keyword_model = KeyBERT("distiluse-base-multilingual-cased-v2")


ReviewType = Literal[
    "course_review",
    "student_evaluation",
    "external_review"
]


class AnalyzeReviewRequest(BaseModel):
    text: str
    reviewType: ReviewType = "course_review"
    rating: Optional[int] = None


class TopicResult(BaseModel):
    name: str
    percent: int
    keywords: List[str]


class AnalyzeReviewResponse(BaseModel):
    sentiment: str
    sentimentScore: float

    positivePercent: int
    negativePercent: int
    neutralPercent: int

    emotion: str
    keywords: str
    topics: List[TopicResult]
    summary: str

    teacherScore: Optional[float] = None
    courseScore: Optional[float] = None
    behaviorScore: Optional[float] = None

    studentRiskScore: Optional[float] = None
    behaviorScoreNlp: Optional[float] = None
    progressScoreNlp: Optional[float] = None

    publicPerceptionScore: Optional[float] = None


teacher_positive = [
    "profesorul explică clar", "explică clar", "răspunde la întrebări",
    "are răbdare", "implicat", "clar", "ajută"
]

teacher_negative = [
    "profesorul nu explică", "explicații neclare", "neclar",
    "nu răspunde", "lipsă de răbdare"
]

course_positive = [
    "bine structurat", "materiale utile", "curs util",
    "ritm bun", "exemple practice", "bine organizat"
]

course_negative = [
    "nu este bine structurat", "slab structurat", "prea rapid",
    "ritm prea rapid", "materiale slabe", "ar trebui îmbunătățiri",
    "îmbunătățiri", "incomplete", "prea puține", "haotic"
]

behavior_positive = [
    "respectuos", "punctual", "activ", "implicat",
    "participă", "atent", "prezent"
]

behavior_negative = [
    "neatent", "întârzie", "deranjează",
    "neimplicat", "absent", "nu participă"
]


topic_dictionaries = {
    "course_review": {
        "Ritm curs": ["rapid", "repede", "greu", "lent", "ritm", "viteză", "încărcat"],
        "Materiale": ["materiale", "suport", "pdf", "slide", "documentație", "resurse", "incomplete"],
        "Calitate profesor": ["explicații", "explică", "clar", "neclar", "profesor", "răbdare", "întrebări"],
        "Structură curs": ["structurat", "organizare", "ordine", "haotic", "capitole", "programă"],
        "Exerciții practice": ["exerciții", "practic", "practice", "aplicații", "teme", "laborator", "exemple"]
    },
    "student_evaluation": {
        "Prezență": ["absent", "absențe", "prezent", "întârzie", "participă"],
        "Comportament": ["respectuos", "deranjează", "neatent", "atent", "disciplinat", "neimplicat"],
        "Progres": ["progres", "evoluează", "înțelege", "nu înțelege", "dificultăți", "se descurcă"],
        "Implicare": ["activ", "implicat", "neimplicat", "participă", "teme", "atenție"]
    },
    "external_review": {
        "Imagine publică": ["recomand", "experiență", "mulțumit", "nemulțumit", "serios", "neserios"],
        "Calitate servicii": ["profesional", "slab", "bun", "excelent", "calitate"],
        "Comunicare": ["comunicare", "răspuns", "suport", "ajutor", "contact"],
        "Preț / valoare": ["preț", "scump", "ieftin", "merită", "valoare"]
    }
}


def contains_term(text: str, term: str) -> bool:
    return re.search(rf"(?<!\w){re.escape(term)}(?!\w)", text, re.IGNORECASE) is not None


def category_score(text: str, positive_terms: list[str], negative_terms: list[str]) -> float:
    text_lower = text.lower()

    negative = sum(1 for term in negative_terms if contains_term(text_lower, term))
    positive = sum(1 for term in positive_terms if contains_term(text_lower, term))

    total = positive + negative

    if total == 0:
        return 0.5

    return round(positive / total, 2)


def has_negative_feedback(text: str) -> bool:
    text_lower = text.lower()

    negative_patterns = [
        "prea rapid", "prea greu", "nu este", "nu e", "slab",
        "problemă", "problema", "imbunatatiri", "îmbunătățiri",
        "ar trebui", "lipsa", "lipsă", "insuficient",
        "incomplete", "prea puține", "neclar", "haotic",
        "nu explică", "nu răspunde", "dificultăți"
    ]

    return any(pattern in text_lower for pattern in negative_patterns)


def has_positive_feedback(text: str) -> bool:
    text_lower = text.lower()

    positive_patterns = [
        "nu a lipsit", "a fost la toate cursurile",
        "participă", "este prezent", "punctual",
        "activ", "excelent", "foarte util",
        "bine structurat", "explică clar", "recomand"
    ]

    return any(pattern in text_lower for pattern in positive_patterns)


def calculate_percentages(sentiment: str, confidence: float):
    confidence_percent = round(confidence * 100)

    if sentiment == "pozitiv":
        positive = confidence_percent
        negative = 0
        neutral = 100 - confidence_percent

    elif sentiment == "negativ":
        positive = 0
        negative = confidence_percent
        neutral = 100 - confidence_percent

    else:
        neutral = confidence_percent
        positive = round((100 - neutral) / 2)
        negative = 100 - neutral - positive

    return normalize_percentages(positive, negative, neutral)


def normalize_percentages(positive: int, negative: int, neutral: int):
    total = positive + negative + neutral

    if total == 0:
        return 0, 0, 100

    positive = round(positive / total * 100)
    negative = round(negative / total * 100)
    neutral = 100 - positive - negative

    return positive, negative, neutral


def detect_topics(text: str, review_type: ReviewType) -> List[TopicResult]:
    text_lower = text.lower()
    dictionary = topic_dictionaries.get(review_type, topic_dictionaries["course_review"])

    topic_counts = {}

    for topic, terms in dictionary.items():
        count = sum(1 for term in terms if term in text_lower)

        if count > 0:
            topic_counts[topic] = count

    total = sum(topic_counts.values())

    if total == 0:
        return []

    topics = []

    for topic, count in topic_counts.items():
        topics.append(
            TopicResult(
                name=topic,
                percent=round((count / total) * 100),
                keywords=[
                    term for term in dictionary[topic]
                    if term in text_lower
                ]
            )
        )

    return sorted(topics, key=lambda x: x.percent, reverse=True)


def detect_emotion(sentiment: str, text: str) -> str:
    text_lower = text.lower()

    if any(word in text_lower for word in ["excelent", "super", "foarte util", "mulțumit", "recomand"]):
        return "mulțumire"

    if any(word in text_lower for word in ["greu", "neclar", "confuz", "nu înțeleg", "prea rapid", "dificultăți"]):
        return "frustrare"

    if any(word in text_lower for word in ["plictisitor", "lent", "monoton"]):
        return "plictiseală"

    if sentiment == "pozitiv":
        return "satisfacție"

    if sentiment == "negativ":
        return "nemulțumire"

    return "neutru"


def calculate_student_risk(text: str, sentiment: str) -> float:
    text_lower = text.lower()

    risk = 0.0

    risk_terms = [
        "absent", "absențe", "întârzie", "neatent",
        "nu participă", "nu înțelege", "progres slab",
        "neimplicat", "risc", "renunță", "dificultăți"
    ]

    for term in risk_terms:
        if term in text_lower:
            risk += 20

    if sentiment == "negativ":
        risk += 30

    return min(round(risk, 2), 100.0)


def calculate_progress_score_nlp(text: str) -> float:
    positive_terms = [
        "progres bun", "evoluează", "se descurcă",
        "înțelege", "activ", "progresează"
    ]

    negative_terms = [
        "progres slab", "nu înțelege", "dificultăți",
        "nu se descurcă", "stagnează"
    ]

    return category_score(text, positive_terms, negative_terms)


def calculate_public_perception(sentiment: str, confidence: float) -> float:
    if sentiment == "pozitiv":
        return round(confidence, 2)

    if sentiment == "negativ":
        return round(1 - confidence, 2)

    return 0.5


def generate_summary(sentiment: str, topics: List[TopicResult], review_type: ReviewType) -> str:
    if not topics:
        if sentiment == "pozitiv":
            return "Feedback pozitiv, fără probleme clare identificate."
        if sentiment == "negativ":
            return "Feedback negativ, dar fără un subiect clar identificat."
        return "Feedback neutru, fără suficiente informații pentru o analiză detaliată."

    main_topic = topics[0].name

    if review_type == "course_review":
        if sentiment == "negativ":
            return f"Feedback-ul indică probleme legate de {main_topic}."
        if sentiment == "pozitiv":
            return f"Feedback pozitiv pentru curs, cu accent pe {main_topic}."
        return f"Feedback mixt pentru curs, principalele observații fiind legate de {main_topic}."

    if review_type == "student_evaluation":
        if sentiment == "negativ":
            return f"Evaluarea indică posibile probleme ale cursantului legate de {main_topic}."
        if sentiment == "pozitiv":
            return f"Evaluare pozitivă a cursantului, cu accent pe {main_topic}."
        return f"Evaluare mixtă a cursantului, cu observații legate de {main_topic}."

    if review_type == "external_review":
        if sentiment == "negativ":
            return f"Review extern negativ, cu impact posibil asupra percepției publice: {main_topic}."
        if sentiment == "pozitiv":
            return f"Review extern pozitiv, cu accent pe {main_topic}."
        return f"Review extern neutru sau mixt, cu observații legate de {main_topic}."

    return "Feedback analizat cu succes."


def extract_keywords(text: str) -> str:
    try:
        keywords_result = keyword_model.extract_keywords(
            text,
            keyphrase_ngram_range=(1, 2),
            stop_words=None,
            top_n=5
        )

        return ", ".join([kw[0] for kw in keywords_result])

    except Exception:
        return ""


@app.get("/")
def root():
    return {"message": "NLP HuggingFace Microservice is running"}


@app.post("/analyze-review", response_model=AnalyzeReviewResponse)
def analyze_review(request: AnalyzeReviewRequest):
    text = request.text.strip()
    review_type = request.reviewType

    if not text:
        return AnalyzeReviewResponse(
            sentiment="neutru",
            sentimentScore=0.0,
            positivePercent=0,
            negativePercent=0,
            neutralPercent=100,
            emotion="neutru",
            keywords="",
            topics=[],
            summary="Textul este gol. Nu există informații pentru analiză."
        )

    result = sentiment_model(text)[0]

    label = result["label"]
    confidence = float(result["score"])
    stars = int(label[0])

    if stars <= 2:
        sentiment = "negativ"
    elif stars == 3:
        sentiment = "neutru"
    else:
        sentiment = "pozitiv"

    has_negative = has_negative_feedback(text)
    has_positive = has_positive_feedback(text)

    if has_negative:
        sentiment = "negativ"
        confidence = max(confidence, 0.60)

    if has_positive and has_negative:
        sentiment = "neutru"
        confidence = max(confidence, 0.50)

    positive_percent, negative_percent, neutral_percent = calculate_percentages(
        sentiment,
        confidence
    )

    if has_positive and has_negative:
        positive_percent = max(positive_percent, 30)
        negative_percent = max(negative_percent, 30)
        neutral_percent = 100 - positive_percent - negative_percent

    topics = detect_topics(text, review_type)
    keywords = extract_keywords(text)

    teacher_score = None
    course_score = None
    behavior_score = None

    student_risk_score = None
    behavior_score_nlp = None
    progress_score_nlp = None

    public_perception_score = None

    if review_type == "course_review":
        teacher_score = category_score(text, teacher_positive, teacher_negative)
        course_score = category_score(text, course_positive, course_negative)
        behavior_score = category_score(text, behavior_positive, behavior_negative)

    elif review_type == "student_evaluation":
        behavior_score = category_score(text, behavior_positive, behavior_negative)
        student_risk_score = calculate_student_risk(text, sentiment)
        behavior_score_nlp = behavior_score
        progress_score_nlp = calculate_progress_score_nlp(text)

    elif review_type == "external_review":
        public_perception_score = calculate_public_perception(sentiment, confidence)

    return AnalyzeReviewResponse(
        sentiment=sentiment,
        sentimentScore=round(confidence, 2),

        positivePercent=positive_percent,
        negativePercent=negative_percent,
        neutralPercent=neutral_percent,

        emotion=detect_emotion(sentiment, text),
        keywords=keywords,
        topics=topics,
        summary=generate_summary(sentiment, topics, review_type),

        teacherScore=teacher_score,
        courseScore=course_score,
        behaviorScore=behavior_score,

        studentRiskScore=student_risk_score,
        behaviorScoreNlp=behavior_score_nlp,
        progressScoreNlp=progress_score_nlp,

        publicPerceptionScore=public_perception_score
    )