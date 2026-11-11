
export async function onRequest(context) {
    const { request, env } = context;

    const apiOrigin = env.API_ORIGIN;

    if (!apiOrigin) {
        return new Response(
            'API_ORIGIN no configurado.',
            { status: 503 }
        );
    }

    let backendOrigin;

    try {
        backendOrigin = new URL(apiOrigin);

        if (
            backendOrigin.protocol !== 'https:' ||
            backendOrigin.username ||
            backendOrigin.password ||
            backendOrigin.pathname !== '/' ||
            backendOrigin.search ||
            backendOrigin.hash
        ) {
            throw new Error('Origen no válido');
        }
    } catch {
        return new Response(
            'Configuración del backend no válida.',
            { status: 503 }
        );
    }

    const incomingUrl = new URL(request.url);

    // Mantener la ruta /api y sus parámetros.
    const targetUrl = new URL(
        incomingUrl.pathname + incomingUrl.search,
        backendOrigin
    );

    const headers = new Headers(request.headers);

    // Evitar enviar al backend cabeceras de
    // autenticación propias de Cloudflare.
    headers.delete('cf-access-jwt-assertion');
    headers.delete('cf-access-authenticated-user-email');

    // El backend recibe el origen público del navegador.
    headers.set('Origin', incomingUrl.origin);

    const upstreamRequest = new Request(
        targetUrl.toString(),
        {
            method: request.method,
            headers,
            body: request.body,
            redirect: 'manual'
        }
    );

    const upstreamResponse = await fetch(upstreamRequest);

    const responseHeaders =
        new Headers(upstreamResponse.headers);

    // El navegador accede a la API mediante el
    // mismo origen; no necesitamos CORS aquí.
    responseHeaders.delete('access-control-allow-origin');
    responseHeaders.delete('access-control-allow-credentials');

    // No almacenar respuestas de autenticación
    // ni datos privados en cachés intermedias.
    responseHeaders.set('Cache-Control', 'no-store');

    return new Response(upstreamResponse.body, {
        status: upstreamResponse.status,
        headers: responseHeaders
    });
}
