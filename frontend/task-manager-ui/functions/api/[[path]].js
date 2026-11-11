
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
            throw new Error('Origen no valido');
        }
    } catch {
        return new Response(
            'Configuracion del backend no valida.',
            { status: 503 }
        );
    }

    const incomingUrl = new URL(request.url);
    const origin = request.headers.get('Origin');
    const method = request.method.toUpperCase();

    const unsafeMethod = ![
        'GET',
        'HEAD',
        'OPTIONS'
    ].includes(method);

    // No permitir que una web externa utilice
    // el proxy para saltarse la validacion CSRF.
    if (
        unsafeMethod &&
        origin !== incomingUrl.origin
    ) {
        return new Response(
            'Origen no autorizado.',
            { status: 403 }
        );
    }

    const targetUrl = new URL(
        incomingUrl.pathname + incomingUrl.search,
        backendOrigin
    );

    const headers = new Headers(request.headers);

    // No reenviar credenciales de Cloudflare.
    headers.delete('cf-access-jwt-assertion');
    headers.delete('cf-access-authenticated-user-email');

    // Solo establecer un origen validado.
    if (unsafeMethod) {
        headers.set('Origin', incomingUrl.origin);
    } else if (origin) {
        // Conservar el origen real en peticiones
        // que no modifican datos.
        headers.set('Origin', origin);
    }

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

    responseHeaders.delete('access-control-allow-origin');
    responseHeaders.delete('access-control-allow-credentials');

    responseHeaders.set('Cache-Control', 'no-store');

    return new Response(upstreamResponse.body, {
        status: upstreamResponse.status,
        headers: responseHeaders
    });
}
