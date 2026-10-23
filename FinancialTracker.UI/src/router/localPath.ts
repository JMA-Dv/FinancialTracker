/**
 * Keeps only paths on this site, so the sign-in link can't be turned into an open redirect.
 * Anything else becomes the home route.
 */
export function localPath(value: unknown): string {
  if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) {
    return '/'
  }
  return value
}
