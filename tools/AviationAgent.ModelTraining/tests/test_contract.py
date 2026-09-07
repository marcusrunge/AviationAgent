import pytest

from aviation_agent_training.contract import validate_answer


def test_valid_weather_decision() -> None:
    result = validate_answer('{"action":"get_taf","station":"ETHS","focus":"wind"}')
    assert result["station"] == "ETHS"


def test_weather_decision_without_station_is_rejected() -> None:
    with pytest.raises(ValueError):
        validate_answer('{"action":"get_taf","station":null,"focus":"wind"}')
