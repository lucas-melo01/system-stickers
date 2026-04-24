const base = process.env.NEXT_PUBLIC_API_URL ?? "";

export class ApiError extends Error {
  status: number;
  body: string;
  constructor(status: number, body: string) {
    super(`${status}${body ? `: ${body}` : ""}`);
    this.status = status;
    this.body = body;
  }
}

export async function apiGet<T>(path: string, accessToken: string): Promise<T> {
  const r = await fetch(`${base}${path}`, {
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    cache: "no-store",
  });
  if (!r.ok) {
    const t = await r.text();
    throw new ApiError(r.status, t || r.statusText);
  }
  return r.json() as Promise<T>;
}

export async function apiPost<T>(path: string, accessToken: string, body?: unknown): Promise<T> {
  const r = await fetch(`${base}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!r.ok) {
    const t = await r.text();
    throw new Error(t || r.statusText);
  }
  if (r.status === 204) return undefined as T;
  const ct = r.headers.get("content-type");
  if (ct?.includes("application/json")) return r.json() as Promise<T>;
  return (await r.text()) as T;
}

export async function apiPatch<T>(path: string, accessToken: string, body: unknown): Promise<T> {
  const r = await fetch(`${base}${path}`, {
    method: "PATCH",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(body),
  });
  if (!r.ok) {
    const t = await r.text();
    throw new Error(t || r.statusText);
  }
  return r.json() as Promise<T>;
}

export function apiUrl(path: string) {
  return `${base}${path}`;
}
