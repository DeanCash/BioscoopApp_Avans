window.authInterop = {
    login: async function (username, password) {
        const response = await fetch('http://localhost:5033/api/auth/login', {
            method: 'POST',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                username: username,
                password: password
            })
        });

        let data = null;

        try {
            data = await response.json();
        } catch {
        }

        return {
            ok: response.ok,
            status: response.status,
            data: data
        };
    },

    logout: async function () {
        const response = await fetch('http://localhost:5033/api/auth/logout', {
            method: 'POST',
            credentials: 'include'
        });

        return {
            ok: response.ok,
            status: response.status
        };
    }
};