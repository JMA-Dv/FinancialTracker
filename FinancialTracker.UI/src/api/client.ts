export class ApiClientError extends Error {}

/** The API answered 401: there is no signed-in User. */
export class UnauthorizedError extends ApiClientError {
  constructor() {
    super('Not signed in')
    this.name = 'UnauthorizedError'
  }
}

/** The API rejected the input; `errors` maps each field to its messages. */
export class ValidationError extends ApiClientError {
  constructor(readonly errors: Record<string, string[]>) {
    super('Some fields are invalid')
    this.name = 'ValidationError'
  }
}

/** A rule refused the request. The API words `title` and `detail` for people, so show them as-is. */
export class ProblemError extends ApiClientError {
  constructor(
    readonly status: number,
    readonly title: string,
    readonly detail: string,
  ) {
    super(detail || title)
    this.name = 'ProblemError'
  }
}

interface ProblemBody {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

let unauthorizedHandler: () => void = () => {}

/** Registers who clears the session when any call answers 401. */
export function onUnauthorized(handler: () => void): void {
  unauthorizedHandler = handler
}

async function failureFor(response: Response): Promise<ApiClientError> {
  const body = await readJson<ProblemBody>(response)
  if (body?.errors !== undefined) {
    return new ValidationError(body.errors)
  }
  if (body?.title === undefined && body?.detail === undefined) {
    // Not one of the API's own answers (a proxy error page, a crash), so there are no words to pass on.
    return new ProblemError(response.status, '', `The server could not be reached or failed (${response.status}).`)
  }
  return new ProblemError(response.status, body.title ?? '', body.detail ?? '')
}

async function readJson<T>(response: Response): Promise<T | undefined> {
  const text = await response.text()
  if (text === '') {
    return undefined
  }
  try {
    return JSON.parse(text) as T
  } catch {
    // An HTML error page is expected from a proxy; the caller reports the status instead.
    return undefined
  }
}

async function send<T>(method: string, path: string, body?: unknown): Promise<T> {
  const init: RequestInit = { method, credentials: 'include' }
  if (body !== undefined) {
    init.headers = { 'Content-Type': 'application/json' }
    init.body = JSON.stringify(body)
  }

  const response = await fetch(path, init)
  if (response.status === 401) {
    unauthorizedHandler()
    throw new UnauthorizedError()
  }
  if (!response.ok) {
    throw await failureFor(response)
  }
  return (await readJson<T>(response)) as T
}

export const api = {
  get: <T>(path: string): Promise<T> => send<T>('GET', path),
  post: <T = void>(path: string, body?: unknown): Promise<T> => send<T>('POST', path, body),
  put: <T = void>(path: string, body?: unknown): Promise<T> => send<T>('PUT', path, body),
}
