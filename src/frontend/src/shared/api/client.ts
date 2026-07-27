import { getLearnerId } from './identity'

/** RFC 9457 problem+json failure with the backend's stable errorCode (doc 12 §5). */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly errorCode: string,
    detail: string,
  ) {
    super(detail)
    this.name = 'ApiError'
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
}

/** Fetch wrapper for /api/v1: JSON both ways, X-Learner-Id identity, problem+json errors. */
export async function api<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const learnerId = getLearnerId()
  const response = await fetch(`/api/v1${path}`, {
    method: options.method ?? 'GET',
    headers: {
      ...(options.body !== undefined ? { 'Content-Type': 'application/json' } : {}),
      ...(learnerId ? { 'X-Learner-Id': learnerId } : {}),
    },
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  })

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as {
      errorCode?: string
      title?: string
      detail?: string
    } | null
    throw new ApiError(
      response.status,
      problem?.errorCode ?? 'unknown',
      problem?.detail ?? problem?.title ?? `Request failed with status ${response.status}`,
    )
  }

  if (response.status === 204) {
    return undefined as T
  }
  return (await response.json()) as T
}
