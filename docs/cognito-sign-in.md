# ClerkSuite Cognito sign-in

Apartments uses the same staff session as Town Hub. It does not keep its own password form.

|                           |                                                                           |
| ------------------------- | ------------------------------------------------------------------------- |
| Account                   | `818904800844` (profile `tow`)                                            |
| Pool                      | `Townofwiley-staff` (`us-east-2_bHk9UcenK`)                               |
| App client                | `townofwiley-staff-web` (`258binbcvsms0rfqj5g20qakki`)                    |
| Hosted UI                 | `tow-gov-staff.auth.us-east-2.amazoncognito.com`                          |
| Dev callback registered   | `http://localhost:8082/authentication/login-callback`                     |
| Dev logout URI registered | `http://localhost:8082/`                                                  |
| Office callback           | `https://mr-storage.tail57aa13.ts.net:8443/authentication/login-callback` |
| Office logout URI         | `https://mr-storage.tail57aa13.ts.net:8443/`                              |

Cognito allows an HTTP callback only for localhost. `http://mr-storage:8082/authentication/login-callback` is not registered. DSM nginx already owns port 443, so Tailscale Serve uses **8443**. Clerks open Apartments at `https://mr-storage.tail57aa13.ts.net:8443`. Serve terminates TLS and proxies to `http://127.0.0.1:8082`. The app trusts `X-Forwarded-Proto` and `X-Forwarded-Host` only from loopback, so the Cognito redirect URI is that HTTPS origin.

The Town Hub card uses that HTTPS origin. It does not carry a token, email, or role. A cold visit is redirected to the ClerkSuite hosted login. It does not show an apartments password form.

Logout clears the local cookie and sends the browser to `https://tow-gov-staff.auth.us-east-2.amazoncognito.com/logout` with `client_id` and `logout_uri` only.
