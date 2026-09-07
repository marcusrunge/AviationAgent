from __future__ import annotations

import json
import re

SYSTEM_PROMPT = """You are a routing agent for aviation weather requests. Return exactly one compact JSON object with the keys action, station, and focus. Never make operational start, landing, release, minima, or go/no-go decisions."""

ACTIONS = {
    "get_metar", "get_taf", "get_metar_and_taf", "explain_previous_result",
    "filter_previous_result", "unsupported_operational_decision", "unknown",
}
FOCUSES = {
    "full", "wind", "visibility", "clouds", "weather", "temperature",
    "pressure", "validity", "changes", "worst_conditions", "none",
}
WEATHER_ACTIONS = {"get_metar", "get_taf", "get_metar_and_taf"}
ICAO_PATTERN = re.compile(r"^[A-Z]{4}$")


def validate_answer(value: str) -> dict[str, object]:
    payload = json.loads(value)
    if not isinstance(payload, dict) or set(payload) != {"action", "station", "focus"}:
        raise ValueError("Answer must contain exactly action, station, and focus.")
    action, station, focus = payload["action"], payload["station"], payload["focus"]
    if action not in ACTIONS or focus not in FOCUSES:
        raise ValueError("Unsupported action or focus.")
    if station is not None and (not isinstance(station, str) or ICAO_PATTERN.fullmatch(station) is None):
        raise ValueError("Station must be a four-letter uppercase ICAO code or null.")
    if action in WEATHER_ACTIONS and station is None:
        raise ValueError("Weather actions require a station.")
    if action in {"unknown", "unsupported_operational_decision"} and focus != "none":
        raise ValueError("Non-weather decisions require focus none.")
    return payload
