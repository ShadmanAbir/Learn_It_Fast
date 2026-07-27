/** Returns true when the backend readiness probe reports healthy. */
export async function fetchReadiness(): Promise<boolean> {
  const response = await fetch('/health/ready')
  return response.ok
}
