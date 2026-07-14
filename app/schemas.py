from __future__ import annotations

from typing import Annotated, Any, Literal, Union

from pydantic import BaseModel, Field

#the client can send an empty request to create a new session, and the server will respond with a session_id
class CreateSessionRequest(BaseModel):
    headless: bool = True


class SessionResponse(BaseModel):
    session_id: str


class ContextResponse(BaseModel):
    context_id: str


class PageResponse(BaseModel):
    page_id: str


class NavigateRequest(BaseModel):
    url: str


class ActionRequest(BaseModel):
    action: Literal["click", "fill", "select", "check"]
    selector: str
    value: str | None = None
    checked: bool | None = None


class EvaluateRequest(BaseModel):
    expression: str


class EvaluateResponse(BaseModel):
    value: Any


class GeneratePageObjectRequest(BaseModel):
    class_name: str = "GeneratedPage"


class GeneratePageObjectResponse(BaseModel):
    code: str


class SessionSummary(BaseModel):
    session_id: str
    devtools_port: int


class ContextSummary(BaseModel):
    context_id: str
    session_id: str


class PageSummary(BaseModel):
    page_id: str
    context_id: str
    session_id: str


class DiscoveredAction(BaseModel):
    name: str
    kind: Literal["fill", "click", "select", "check"]
    selector: str


class RunFlowRequest(BaseModel):
    session_id: str
    url: str
    generate_page_object: bool = True
    class_name: str = "GeneratedPage"


class RunFlowResponse(BaseModel):
    session_id: str
    context_id: str
    page_id: str
    generated_code: str | None = None
    available_actions: list[DiscoveredAction]


class InteractStep(BaseModel):
    type: Literal["interact"]
    name: str
    value: str | None = None
    checked: bool | None = None


class AssertStep(BaseModel):
    type: Literal["assert"]
    expression: str
    equals: Any


SimulateStep = Annotated[Union[InteractStep, AssertStep], Field(discriminator="type")]


class SimulateRequest(BaseModel):
    actions: list[SimulateStep]
    close_session: bool = False


class SimulatedActionResult(BaseModel):
    type: Literal["interact", "assert"]
    label: str
    status: Literal["passed", "failed"]
    error: str | None = None
    actual: Any | None = None


class SimulateResponse(BaseModel):
    action_results: list[SimulatedActionResult]
