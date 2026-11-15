# ClerkSuite Cognito sign-in

Apartments uses the same staff session as Town Hub. It does not keep its own password form.

| | |
| --- | --- |
| Account | `818904800844` (profile `tow`) |
| Pool | `Townofwiley-staff` (`us-east-2_bHk9UcenK`) |
| App client | `townofwiley-staff-web` (`258binbcvsms0rfqj5g20qakki`) |
| Hosted UI | `tow-gov-staff.auth.us-east-2.amazoncognito.com` |
| Callback registered | `http://localhost:8082/authentication/login-callback` |
| Logout URI registered | `http://localhost:8082/` |

Cognito allows an HTTP callback only for localhost. `http://mr-storage:8082/authentication/login-callback` is rejected. The NAS silent session starts only after an HTTPS callback on this same client is added. Until then, a visit to the NAS still must not show the apartments password form. The page says **Sign in on ClerkSuite**.

The Town Hub card stays `http://mr-storage:8082`. It does not carry a token, email, or role.

Logout clears the local cookie and sends the browser to `https://tow-gov-staff.auth.us-east-2.amazoncognito.com/logout` with `client_id` and `logout_uri` only.
