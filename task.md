# Auth System — Task List

## Giai Đoạn 1 — Foundation (Shared DTOs + Models)
- [ ] Tạo `shared/DTOs/Auth/RegisterRequest.cs`
- [ ] Mở rộng `shared/DTOs/Auth/LoginResponse.cs` (thêm refreshToken, idToken, errorCode)
- [ ] Mở rộng `shared/Models/User.cs` (thêm cognitoSub)
- [ ] Build `GameShared.dll` → copy vào `Assets/Plugins/`

## Giai Đoạn 2 — Plan A: Mock Local (Unity Client)
- [ ] Tạo `Assets/Script/Auth/IUnityAuthService.cs` + `AuthResult.cs`
- [ ] Tạo `Assets/Script/Auth/MockAuthService.cs`
- [ ] Tạo `Assets/Script/Auth/RealAuthService.cs`
- [ ] Tạo `Assets/Script/Auth/AuthManager.cs`
- [ ] Tạo `Assets/Script/Auth/UI/LoginPanelController.cs`
- [ ] Tạo `Assets/Script/Auth/UI/RegisterPanelController.cs`

## Giai Đoạn 3 — Backend Hardening
- [ ] Sửa `backend/.../Utils/JwtHelper.cs` dùng System.IdentityModel.Tokens.Jwt
- [ ] Sửa `backend/.../Services/AuthService.cs` dùng BCrypt
- [ ] Sửa `backend/.../Handlers/Auth/RegisterHandler.cs` dùng RegisterRequest DTO
- [ ] Thêm `GetByEmailAsync()` vào `IUserRepository` + `UserRepository`
- [ ] Thêm NuGet: BCrypt.Net-Next, System.IdentityModel.Tokens.Jwt
- [ ] Sửa GameBackend.Core.csproj thêm packages

## Giai Đoạn 4 — Plan B: AWS Cognito (Infrastructure + Backend)
- [ ] Tạo `infrastructure/.../Stacks/CognitoStack.cs`
- [ ] Sửa `ApiStack.cs` thêm Cognito Authorizer
- [ ] Tạo `backend/.../Services/CognitoAuthService.cs`
- [ ] Sửa `IAuthService.cs` thêm methods Cognito
- [ ] Tạo `RefreshTokenHandler.cs`, `ConfirmSignUpHandler.cs`
- [ ] Sửa `ServiceProviderBuilder.cs` (swap IAuthService → CognitoAuthService)
- [ ] Thêm shared DTOs: RefreshTokenRequest, ConfirmSignUpRequest
- [ ] Tạo `Assets/Script/Auth/CognitoAuthService.cs` (Unity)
- [ ] Tạo `Assets/Script/Auth/UI/ConfirmSignUpController.cs`
- [ ] Sửa `GameConfigSO.cs` thêm Cognito settings
