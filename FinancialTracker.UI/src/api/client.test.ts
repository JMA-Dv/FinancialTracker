import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiClientError, ProblemError, UnauthorizedError, ValidationError, api, onUnauthorized } from './client'

function respond(status: number, body?: unknown): Response {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: body === undefined ? {} : { 'Content-Type': 'application/json' },
  })
}

function fakeFetch(response: Response): ReturnType<typeof vi.fn> {
  const fetchMock = vi.fn().mockResolvedValue(response)
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

afterEach(() => vi.unstubAllGlobals())

describe('API client', () => {
  it('returns the parsed JSON body and sends the session cookie', async () => {
    const fetchMock = fakeFetch(respond(200, { email: 'ana@example.com', currency: null }))

    const settings = await api.get<{ email: string }>('/api/user-settings')

    expect(settings.email).toBe('ana@example.com')
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/user-settings',
      expect.objectContaining({ method: 'GET', credentials: 'include' }),
    )
  })

  it('reports a 401 as an UnauthorizedError and tells the unauthorized handler', async () => {
    fakeFetch(respond(401))
    const handler = vi.fn()
    onUnauthorized(handler)

    const failure = await api.get('/api/accounts').catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(UnauthorizedError)
    expect(failure).toBeInstanceOf(ApiClientError)
    expect(handler).toHaveBeenCalledOnce()
  })

  it('reports a 400 with an errors dictionary as field errors', async () => {
    fakeFetch(
      respond(400, {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: { Name: ['Name is required.'], Currency: ['Unknown currency.', 'Must be 3 letters.'] },
      }),
    )

    const failure = await api.get('/api/accounts').catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(ValidationError)
    expect((failure as ValidationError).errors).toEqual({
      Name: ['Name is required.'],
      Currency: ['Unknown currency.', 'Must be 3 letters.'],
    })
  })

  it.each([400, 404, 409])('shows the API\'s own words for a %i ProblemDetails', async (status) => {
    fakeFetch(respond(status, { status, title: 'Account not closable', detail: 'The balance must be zero.' }))

    const failure = await api.get('/api/accounts/1').catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(ProblemError)
    expect(failure).toMatchObject({
      status,
      title: 'Account not closable',
      detail: 'The balance must be zero.',
      message: 'The balance must be zero.',
    })
  })

  it('sends a JSON body and accepts an empty 204 answer', async () => {
    const fetchMock = fakeFetch(respond(204))

    const result = await api.post('/api/auth/sign-out')
    await api.post('/api/user-settings/currency', { currency: 'USD' })

    expect(result).toBeUndefined()
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/user-settings/currency',
      expect.objectContaining({
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ currency: 'USD' }),
      }),
    )
  })

  it('still reports a failure whose body is not JSON', async () => {
    fakeFetch(new Response('<html>Bad gateway</html>', { status: 502 }))

    const failure = await api.get('/api/accounts').catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(ProblemError)
    expect(failure).toMatchObject({ status: 502, message: 'The server could not be reached or failed (502).' })
  })

  it('reports an unreachable server as a failure the User can read', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    const failure = await api.get('/api/accounts').catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(ProblemError)
    expect((failure as ProblemError).message).toBe('Could not reach the server. Check your connection and try again.')
  })
})
