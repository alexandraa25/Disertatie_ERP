from fastapi import FastAPI
from pydantic import BaseModel
from transformers import pipeline
from keybert import KeyBERT
from typing import List

app = FastAPI()

sentiment_model = pipeline(
    "sentiment-analysis",
    model="nlptown/bert-base-multilingual-uncased-sentiment"
)

keyword_model = KeyBERT("distiluse-base-multilingual-cased-v2")


class AnalyzeReviewRequest(BaseModel):
    text: str
    reviewType: str = "course_review"


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

    teacherScore: float
    courseScore: float
    behaviorScore: float

    studentRiskScore: float
    behaviorScoreNlp: float
    progressScoreNlp: float

    publicPerceptionScore: float

    topics: List[TopicResult]
    sentiment: str
    sentimentScore: float
    positivePercent: int
    negativePercent: int
    neutralPercent: int
    emotion: str
    keywords: str
    teacherScore: float
    courseScore: float
    behaviorScore: float
    topics: List[TopicResult]


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
    "ritm bun", "exemple practice"
]

course_negative = [
    "nu este bine structurat", "slab structurat", "prea rapid",
    "ritm prea rapid", "materiale slabe", "ar trebui îmbunătățiri",
    "îmbunătățiri", "incomplete", "prea puține"
]

behavior_positive = [
    "respectuos", "punctual", "activ", "implicat", "participă", "atent"
]

behavior_negative = [
    "neatent", "întârzie", "deranjează", "neimplicat", "absent"
]

topic_dictionary = {
    "Ritm curs": ["rapid", "repede", "greu", "lent", "ritm", "viteză", "încărcat"],
    "Materiale": ["materiale", "suport", "pdf", "slide", "documentație", "resurse", "incomplete"],
    "Calitate profesor": ["explicații", "explică", "clar", "neclar", "profesor", "răbdare", "întrebări"],
    "Structură curs": ["structurat", "organizare", "ordine", "haotic", "capitole", "programă"],
    "Exerciții practice": ["exerciții", "practic", "practice", "aplicații", "teme", "laborator", "exemple"]
}


def category_score(text: str, positive_terms: list[str], negative_terms: list[str]) -> float:
    text_lower = text.lower()

    positive = sum(1 for term in positive_terms if term in text_lower)
    negative = sum(1 for term in negative_terms if term in text_lower)

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
        "incomplete", "prea puține", "neclar"
    ]

    return any(pattern in text_lower for pattern in negative_patterns)

def has_positive_feedback(text: str) -> bool:
    text = text.lower()

    positive_patterns = [
        "nu a lipsit",
        "a fost la toate cursurile",
        "participă",
        "este prezent",
        "punctual",
        "activ"
    ]

    return any(p in text for p in positive_patterns)

def calculate_percentages(sentiment: str, confidence: float):
    confidence_percent = round(confidence * 100)

    if sentiment == "pozitiv":
        return confidence_percent, 0, 100 - confidence_percent

    if sentiment == "negativ":
        return 0, confidence_percent, 100 - confidence_percent

    neutral = confidence_percent
    positive = round((100 - neutral) / 2)
    negative = 100 - neutral - positive

    return positive, negative, neutral


def detect_topics(text: str) -> List[TopicResult]:
    text_lower = text.lower()
    topic_counts = {}

    for topic, terms in topic_dictionary.items():
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
                    term for term in topic_dictionary[topic]
                    if term in text_lower
                ]
            )
        )

    return sorted(topics, key=lambda x: x.percent, reverse=True)


def detect_emotion(sentiment: str, text: str) -> str:
    text_lower = text.lower()

    if any(word in text_lower for word in ["excelent", "super", "foarte util", "mulțumit"]):
        return "mulțumire"

    if any(word in text_lower for word in ["greu", "neclar", "confuz", "nu înțeleg", "prea rapid"]):
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
        "absent", "absențe", "întârzie", "neatent", "nu participă",
        "nu înțelege", "progres slab", "neimplicat", "risc", "renunță"
    ]

    for term in risk_terms:
        if term in text_lower:
            risk += 20

    if sentiment == "negativ":
        risk += 30

    return min(round(risk, 2), 100.0)


def calculate_progress_score_nlp(text: str) -> float:
    text_lower = text.lower()

    positive_terms = ["progres bun", "evoluează", "se descurcă", "înțelege", "activ"]
    negative_terms = ["progres slab", "nu înțelege", "dificultăți", "nu se descurcă"]

    return category_score(text_lower, positive_terms, negative_terms)


def calculate_public_perception(sentiment: str, confidence: float) -> float:
    if sentiment == "pozitiv":
        return round(confidence, 2)

    if sentiment == "negativ":
        return round(1 - confidence, 2)

    return 0.5

def generate_summary(text: str, sentiment: str, topics: List[TopicResult]) -> str:
    text_lower = text.lower()

    if not topics:
        return "Nu există suficiente informații pentru analiză."

    main_topic = topics[0].name

    if sentiment == "negativ":
        return f"Feedback-ul indică probleme legate de {main_topic}."

    if sentiment == "pozitiv":
        return f"Feedback pozitiv, cu accent pe {main_topic}."

    return f"Feedback mixt, principalele observații fiind legate de {main_topic}."

@app.get("/")
def root():
    return {"message": "NLP HuggingFace Microservice is running"}


@app.post("/analyze-review", response_model=AnalyzeReviewResponse)
def analyze_review(request: AnalyzeReviewRequest):
    text = request.text.strip()

    if not text:
        return AnalyzeReviewResponse(
           sentiment="neutru",
           sentimentScore=0.0,
           positivePercent=0,
           negativePercent=0,
           neutralPercent=100,
           emotion="neutru",
           keywords="",
           teacherScore=0.5,
           courseScore=0.5,
           behaviorScore=0.5,
           studentRiskScore=0.0,
           behaviorScoreNlp=0.5,
           progressScoreNlp=0.5,
           publicPerceptionScore=0.5,
           topics=[]
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

    if has_negative_feedback(text):
        sentiment = "negativ"
        confidence = max(confidence, 0.60)

    if has_positive_feedback(text) and has_negative_feedback(text):
       sentiment = "neutru"

    positive_percent, negative_percent, neutral_percent = calculate_percentages(
        sentiment,
        confidence
    )

    if has_positive_feedback(text):
       positive_percent = max(positive_percent, 30)
       negative_percent = max(negative_percent, 40)

    keywords_result = keyword_model.extract_keywords(
        text,
        keyphrase_ngram_range=(1, 2),
        stop_words=None,
        top_n=5
    )

    keywords = ", ".join([kw[0] for kw in keywords_result])

    behavior_score = category_score(text, behavior_positive, behavior_negative)
    progress_score_nlp = calculate_progress_score_nlp(text)
    student_risk_score = calculate_student_risk(text, sentiment)
    public_perception_score = calculate_public_perception(sentiment, confidence)

    return AnalyzeReviewResponse(
       sentiment=sentiment,
       sentimentScore=round(confidence, 2),
       positivePercent=positive_percent,
       negativePercent=negative_percent,
       neutralPercent=neutral_percent,
       emotion=detect_emotion(sentiment, text),
       keywords=keywords,

       teacherScore=category_score(text, teacher_positive, teacher_negative),
       courseScore=category_score(text, course_positive, course_negative),
       behaviorScore=behavior_score,

       studentRiskScore=student_risk_score,
       behaviorScoreNlp=behavior_score,
       progressScoreNlp=progress_score_nlp,

       publicPerceptionScore=public_perception_score,

       topics=detect_topics(text), 
       summary = generate_summary(text, sentiment, topics)
    )

