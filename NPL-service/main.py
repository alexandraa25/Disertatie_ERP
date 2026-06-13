from fastapi import FastAPI
from pydantic import BaseModel, Field
from transformers import pipeline
from keybert import KeyBERT
from typing import List, Optional, Literal, Dict, Tuple
import re
import unicodedata

app = FastAPI(title="Review NLP Microservice", version="2.0.0")

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

SentimentType = Literal["pozitiv", "negativ", "neutru"]

class AnalyzeReviewRequest(BaseModel):
    text: str = Field(..., min_length=0)
    reviewType: ReviewType = "course_review"
    rating: Optional[int] = Field(default=None, ge=1, le=5)


class TopicResult(BaseModel):
    name: str
    percent: int
    keywords: List[str]


class AnalyzeReviewResponse(BaseModel):
    sentiment: SentimentType
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


ROMANIAN_STOP_WORDS = [
    "si", "sau", "dar", "iar", "este", "sunt", "fost", "foarte", "mai",
    "mult", "multe", "putea", "ar", "la", "de", "din", "cu", "pe", "pentru",
    "care", "daca", "toate", "toti", "toata", "un", "o", "in", "insa", "prin"
]

DIACRITIC_MAP = str.maketrans({
    "ă": "a", "â": "a", "î": "i", "ș": "s", "ş": "s", "ț": "t", "ţ": "t",
    "Ă": "a", "Â": "a", "Î": "i", "Ș": "s", "Ş": "s", "Ț": "t", "Ţ": "t",
})


def normalize_text(text: str) -> str:
    text = text.strip().lower()
    text = text.translate(DIACRITIC_MAP)
    text = unicodedata.normalize("NFKC", text)
    text = re.sub(r"\s+", " ", text)
    return text


def split_sentences(text: str) -> List[str]:
    parts = re.split(r"(?<=[.!?])\s+|\n+", text.strip())
    return [p.strip() for p in parts if p.strip()]


def contains_term(normalized_text: str, term: str) -> bool:
    normalized_term = normalize_text(term)
    return re.search(rf"(?<!\w){re.escape(normalized_term)}(?!\w)", normalized_text) is not None


def count_terms(normalized_text: str, terms: List[str]) -> int:
    return sum(1 for term in terms if contains_term(normalized_text, term))


teacher_positive = [
    "profesorul explica clar", "explica clar", "explicatii clare", "claritate buna",
    "raspunde la intrebari", "raspunde rapid", "raspunde bine", "ofera exemple",
    "are rabdare", "rabdator", "implicat", "dedicat", "ajuta", "suport bun",
    "cunoaste foarte bine", "cunoaste materia", "stapaneste materia", "bine pregatit",
    "profesionist", "comunica bine", "verifica intelegerea", "se asigura ca am inteles",
    "explica pe inteles", "deschis la intrebari", "feedback util"
]

teacher_negative = [
    "profesorul nu explica", "nu explica", "explicatii neclare", "neclar",
    "nu raspunde", "raspunde greu", "lipsa de rabdare", "fara rabdare",
    "vorbeste prea repede", "prea repede", "grabeste", "se grabeste",
    "nu verifica", "nu verifica daca", "nu se asigura", "nu intreaba daca am inteles",
    "nu ofera exemple", "prea teoretic", "lipsa exemple", "nu ajuta",
    "slab pregatit", "nu cunoaste materia", "comunicare slaba", "ton nepotrivit",
    "nu se implica", "dezinteresat", "superficial"
]

course_positive = [
    "bine structurat", "structura buna", "bine organizat", "organizare buna",
    "materiale utile", "materialele sunt utile", "suport util", "resurse utile",
    "curs util", "foarte util", "ritm bun", "ritm potrivit", "exemple practice",
    "exercitii practice", "aplicatii practice", "teme utile", "continut relevant",
    "informatii clare", "programa buna", "lectii interactive", "sesiuni interactive",
    "usor de urmarit", "bine explicat", "continut actualizat"
]

course_negative = [
    "nu este bine structurat", "nu e bine structurat", "slab structurat", "haotic",
    "dezorganizat", "organizare slaba", "prea rapid", "ritm prea rapid", "prea lent",
    "ritm nepotrivit", "prea incarcat", "prea greu", "materiale slabe", "materiale incomplete",
    "suport incomplet", "resurse insuficiente", "ar trebui imbunatatiri", "imbunatatiri",
    "incomplete", "prea putine", "prea putine exercitii", "mai multe exercitii",
    "ar putea include mai multe exercitii", "lipsa exercitii", "prea teoretic",
    "lipsa practica", "nu este interactiv", "sesiuni neinteractive", "plictisitor",
    "continut invechit", "nu este actualizat", "programa neclara"
]

behavior_positive = [
    "respectuos", "punctual", "activ", "implicat", "participa", "atent", "prezent",
    "colaboreaza", "disciplinat", "serios", "isi face temele", "comunica bine",
    "progres bun", "evolueaza", "se descurca", "intelege", "motivat"
]

behavior_negative = [
    "neatent", "intarzie", "deranjeaza", "neimplicat", "absent", "nu participa",
    "lipseste", "absente", "nu este atent", "nu isi face temele", "comportament nepotrivit",
    "nu colaboreaza", "progres slab", "nu intelege", "dificultati", "stagneaza",
    "risc de abandon", "renunta", "demotivat"
]

external_positive = [
    "recomand", "experienta buna", "experienta excelenta", "multumit", "foarte multumit",
    "profesional", "serios", "calitate buna", "excelent", "bun", "merita",
    "pret corect", "valoare buna", "comunicare buna", "suport bun"
]

external_negative = [
    "nu recomand", "experienta proasta", "nemultumit", "foarte nemultumit",
    "neprofesional", "neserios", "calitate slaba", "slab", "scump", "nu merita",
    "comunicare slaba", "nu raspunde", "suport slab", "probleme", "dezamagitor"
]

positive_patterns = sorted(set(
    teacher_positive + course_positive + behavior_positive + external_positive + [
        "excelent", "super", "foarte bun", "bun", "util", "clar", "recomand",
        "mi-a placut", "apreciez", "ok", "perfect", "multumit", "satisfacut"
    ]
), key=len, reverse=True)

negative_patterns = sorted(set(
    teacher_negative + course_negative + behavior_negative + external_negative + [
        "nu este", "nu e", "slab", "problema", "probleme", "lipsa", "insuficient",
        "greu", "confuz", "nemultumit", "dezamagit", "rau", "prost", "neclar",
        "ar trebui", "as vrea", "mi-as dori", "poate fi imbunatatit", "de imbunatatit"
    ]
), key=len, reverse=True)

mixed_markers = [
    "dar", "insa", "totusi", "cu toate acestea", "pe de alta parte", "desi", "in schimb"
]

topic_dictionaries: Dict[str, Dict[str, List[str]]] = {
    "course_review": {
        "Ritm curs": ["rapid", "repede", "greu", "lent", "ritm", "viteza", "incarcat", "prea rapid", "prea lent"],
        "Materiale": ["materiale", "suport", "pdf", "slide", "documentatie", "resurse", "incomplete", "materiale utile", "materiale slabe"],
        "Calitate profesor": ["explicatii", "explica", "clar", "neclar", "profesor", "rabdare", "intrebari", "vorbeste", "verifica"],
        "Structura curs": ["structurat", "organizare", "ordine", "haotic", "capitole", "programa", "dezorganizat"],
        "Exercitii practice": ["exercitii", "practic", "practice", "aplicatii", "teme", "laborator", "exemple", "mai multe exercitii"],
        "Interactivitate": ["interactiv", "interactive", "discutii", "dialog", "participare", "implicare", "sesiuni interactive"]
    },
    "student_evaluation": {
        "Prezenta": ["absent", "absente", "prezent", "intarzie", "participa", "lipseste"],
        "Comportament": ["respectuos", "deranjeaza", "neatent", "atent", "disciplinat", "neimplicat", "comportament"],
        "Progres": ["progres", "evolueaza", "intelege", "nu intelege", "dificultati", "se descurca", "stagneaza"],
        "Implicare": ["activ", "implicat", "neimplicat", "participa", "teme", "atentie", "motivat"]
    },
    "external_review": {
        "Imagine publica": ["recomand", "experienta", "multumit", "nemultumit", "serios", "neserios", "imagine"],
        "Calitate servicii": ["profesional", "slab", "bun", "excelent", "calitate", "servicii"],
        "Comunicare": ["comunicare", "raspuns", "suport", "ajutor", "contact", "nu raspunde"],
        "Pret / valoare": ["pret", "scump", "ieftin", "merita", "valoare", "cost"]
    }
}

emotion_patterns = {
    "entuziasm": ["excelent", "super", "foarte bun", "perfect", "minunat"],
    "multumire": ["multumit", "recomand", "apreciez", "util", "bun"],
    "frustrare": ["greu", "neclar", "confuz", "nu inteleg", "prea rapid", "dificultati", "probleme"],
    "dezamagire": ["dezamagit", "nu recomand", "slab", "prost", "nu merita"],
    "plictiseala": ["plictisitor", "lent", "monoton", "neinteractiv"],
    "ingrijorare": ["risc", "renunta", "absente", "progres slab", "nu participa"]
}


def get_signal_counts(text: str) -> Tuple[int, int, bool]:
    normalized = normalize_text(text)
    positive = count_terms(normalized, positive_patterns)
    negative = count_terms(normalized, negative_patterns)
    is_mixed = positive > 0 and negative > 0
    if any(contains_term(normalized, marker) for marker in mixed_markers) and (positive + negative) > 0:
        is_mixed = True
    return positive, negative, is_mixed


def category_score(text: str, positive_terms: List[str], negative_terms: List[str]) -> float:
    normalized = normalize_text(text)
    positive = count_terms(normalized, positive_terms)
    negative = count_terms(normalized, negative_terms)

    if positive == 0 and negative == 0:
        return 0.5

    raw = 0.5 + positive * 0.12 - negative * 0.12
    return round(max(0.0, min(raw, 1.0)), 2)


def calculate_percentages(sentiment: SentimentType, confidence: float, positive_hits: int, negative_hits: int, rating: Optional[int]) -> Tuple[int, int, int]:
    if positive_hits + negative_hits > 0:
        total = positive_hits + negative_hits
        positive = round(positive_hits / total * 70)
        negative = round(negative_hits / total * 70)
        neutral = 30

        if sentiment == "pozitiv":
            positive += 10
            neutral -= 10
        elif sentiment == "negativ":
            negative += 10
            neutral -= 10
        elif sentiment == "neutru":
            neutral += 10

        if rating is not None:
            if rating >= 4:
                positive += 10
                neutral -= 10
            elif rating <= 2:
                negative += 10
                neutral -= 10
            else:
                neutral += 10

        return normalize_percentages(positive, negative, neutral)

    confidence_percent = round(confidence * 100)
    if sentiment == "pozitiv":
        return normalize_percentages(confidence_percent, 0, 100 - confidence_percent)
    if sentiment == "negativ":
        return normalize_percentages(0, confidence_percent, 100 - confidence_percent)
    return normalize_percentages(20, 20, 60)


def normalize_percentages(positive: int, negative: int, neutral: int) -> Tuple[int, int, int]:
    positive = max(0, positive)
    negative = max(0, negative)
    neutral = max(0, neutral)
    total = positive + negative + neutral
    if total == 0:
        return 0, 0, 100
    positive = round(positive / total * 100)
    negative = round(negative / total * 100)
    neutral = 100 - positive - negative
    return positive, negative, neutral


def sentiment_from_model(label: str) -> SentimentType:
    stars = int(label[0])
    if stars <= 2:
        return "negativ"
    if stars == 3:
        return "neutru"
    return "pozitiv"


def adjust_sentiment(model_sentiment: SentimentType, confidence: float, positive_hits: int, negative_hits: int, is_mixed: bool, rating: Optional[int]) -> Tuple[SentimentType, float]:
    sentiment = model_sentiment

    if is_mixed:
        sentiment = "neutru"
        confidence = max(confidence, 0.60)
    elif negative_hits > positive_hits:
        sentiment = "negativ"
        confidence = max(confidence, 0.65)
    elif positive_hits > negative_hits:
        sentiment = "pozitiv"
        confidence = max(confidence, 0.65)

    if rating is not None:
        if rating >= 4 and sentiment == "negativ":
            sentiment = "neutru" if negative_hits else "pozitiv"
            confidence = max(confidence, 0.60)
        elif rating <= 2 and sentiment == "pozitiv":
            sentiment = "neutru" if positive_hits else "negativ"
            confidence = max(confidence, 0.60)
        elif rating == 3 and positive_hits and negative_hits:
            sentiment = "neutru"
            confidence = max(confidence, 0.60)

    return sentiment, round(min(confidence, 0.99), 2)


def detect_topics(text: str, review_type: ReviewType) -> List[TopicResult]:
    normalized = normalize_text(text)
    dictionary = topic_dictionaries.get(review_type, topic_dictionaries["course_review"])
    topic_counts: Dict[str, int] = {}
    topic_keywords: Dict[str, List[str]] = {}

    for topic, terms in dictionary.items():
        matches = [term for term in terms if contains_term(normalized, term)]
        if matches:
            topic_counts[topic] = len(matches)
            topic_keywords[topic] = matches[:6]

    total = sum(topic_counts.values())
    if total == 0:
        return []

    topics = [
        TopicResult(
            name=topic,
            percent=round(count / total * 100),
            keywords=topic_keywords[topic]
        )
        for topic, count in topic_counts.items()
    ]

    topics.sort(key=lambda item: item.percent, reverse=True)
    return topics


def detect_emotion(sentiment: SentimentType, text: str) -> str:
    normalized = normalize_text(text)
    for emotion, patterns in emotion_patterns.items():
        if any(contains_term(normalized, pattern) for pattern in patterns):
            return emotion
    if sentiment == "pozitiv":
        return "satisfactie"
    if sentiment == "negativ":
        return "nemultumire"
    return "neutru"


def generate_summary(sentiment: SentimentType, topics: List[TopicResult], review_type: ReviewType) -> str:
    if not topics:
        if sentiment == "pozitiv":
            return "Feedback pozitiv, fara probleme clare identificate."
        if sentiment == "negativ":
            return "Feedback negativ, dar fara un subiect clar identificat."
        return "Feedback neutru sau mixt, fara suficiente detalii pentru o analiza precisa."

    main_topics = ", ".join([topic.name for topic in topics[:3]])

    if review_type == "course_review":
        if sentiment == "pozitiv":
            return f"Feedback pozitiv pentru curs, cu aprecieri legate de {main_topics}."
        if sentiment == "negativ":
            return f"Feedback negativ pentru curs, cu probleme semnalate la {main_topics}."
        return f"Feedback mixt pentru curs: exista aprecieri, dar si sugestii de imbunatatire legate de {main_topics}."

    if review_type == "student_evaluation":
        if sentiment == "pozitiv":
            return f"Evaluare pozitiva a cursantului, cu puncte bune la {main_topics}."
        if sentiment == "negativ":
            return f"Evaluarea indica posibile probleme ale cursantului la {main_topics}."
        return f"Evaluare mixta a cursantului, cu observatii legate de {main_topics}."

    if review_type == "external_review":
        if sentiment == "pozitiv":
            return f"Review extern pozitiv, cu impact bun asupra perceptiei publice: {main_topics}."
        if sentiment == "negativ":
            return f"Review extern negativ, cu impact posibil asupra perceptiei publice: {main_topics}."
        return f"Review extern neutru sau mixt, cu observatii legate de {main_topics}."

    return "Feedback analizat cu succes."


def calculate_student_risk(text: str, sentiment: SentimentType) -> float:
    normalized = normalize_text(text)
    risk_terms = [
        "absent", "absente", "lipseste", "intarzie", "neatent", "nu participa",
        "nu intelege", "progres slab", "neimplicat", "risc", "renunta",
        "dificultati", "nu isi face temele", "stagneaza", "demotivat"
    ]
    risk = count_terms(normalized, risk_terms) * 15
    if sentiment == "negativ":
        risk += 25
    elif sentiment == "neutru":
        risk += 10
    return min(round(risk, 2), 100.0)


def calculate_progress_score_nlp(text: str) -> float:
    positive_terms = ["progres bun", "evolueaza", "se descurca", "intelege", "activ", "progreseaza", "rezultate bune"]
    negative_terms = ["progres slab", "nu intelege", "dificultati", "nu se descurca", "stagneaza", "rezultate slabe"]
    return category_score(text, positive_terms, negative_terms)


def calculate_public_perception(sentiment: SentimentType, confidence: float, positive_hits: int, negative_hits: int) -> float:
    if positive_hits + negative_hits > 0:
        return round(positive_hits / (positive_hits + negative_hits), 2)
    if sentiment == "pozitiv":
        return round(confidence, 2)
    if sentiment == "negativ":
        return round(1 - confidence, 2)
    return 0.5



def fallback_keywords(text: str, top_n: int = 5) -> str:
    normalized = normalize_text(text)
    words = re.findall(r"\b[a-z0-9]{3,}\b", normalized)
    words = [word for word in words if word not in ROMANIAN_STOP_WORDS]
    freq: Dict[str, int] = {}
    for word in words:
        freq[word] = freq.get(word, 0) + 1
    sorted_words = sorted(freq.items(), key=lambda item: item[1], reverse=True)
    return ", ".join([word for word, _ in sorted_words[:top_n]])


def extract_keywords(text: str) -> str:
    try:
        keywords_result = keyword_model.extract_keywords(
            text,
            keyphrase_ngram_range=(1, 3),
            stop_words=ROMANIAN_STOP_WORDS,
            top_n=7,
            use_mmr=True,
            diversity=0.6
        )
        keywords = [kw[0] for kw in keywords_result if kw and kw[0]]
        if keywords:
            return ", ".join(keywords)
    except Exception:
        pass
    return fallback_keywords(text)



@app.get("/")
def root():
    return {"message": "NLP HuggingFace Microservice is running", "version": "2.0.0"}


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
            summary="Textul este gol. Nu exista informatii pentru analiza."
        )

    model_result = sentiment_model(text[:512])[0]
    model_sentiment = sentiment_from_model(model_result["label"])
    model_confidence = float(model_result["score"])

    positive_hits, negative_hits, is_mixed = get_signal_counts(text)
    sentiment, confidence = adjust_sentiment(
        model_sentiment=model_sentiment,
        confidence=model_confidence,
        positive_hits=positive_hits,
        negative_hits=negative_hits,
        is_mixed=is_mixed,
        rating=request.rating
    )

    positive_percent, negative_percent, neutral_percent = calculate_percentages(
        sentiment=sentiment,
        confidence=confidence,
        positive_hits=positive_hits,
        negative_hits=negative_hits,
        rating=request.rating
    )

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
        public_perception_score = calculate_public_perception(
            sentiment=sentiment,
            confidence=confidence,
            positive_hits=positive_hits,
            negative_hits=negative_hits
        )

    return AnalyzeReviewResponse(
        sentiment=sentiment,
        sentimentScore=confidence,
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
