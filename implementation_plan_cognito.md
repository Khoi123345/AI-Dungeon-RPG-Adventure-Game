# Kế Hoạch: AWS Cognito Authentication (Giai Đoạn 4)

Kế hoạch này tập trung vào **Giai Đoạn 4 — AWS Cognito Integration**, tích hợp AWS Cognito User Pool làm Identity Provider cho RPG Adventure Game.

## 💰 Chi Phí Trên AWS: Hoàn Toàn Miễn Phí (Free Tier)
AWS Cognito có chính sách giá cực kỳ ưu đãi cho các nhà phát triển game độc lập và dự án nhỏ:
- **50,000 MAUs (Monthly Active Users)** miễn phí mỗi tháng cho tính năng User Pool cơ bản.
- Xác nhận tài khoản bằng **Email Verification là miễn phí** (không dùng SMS vì SMS sẽ tốn tiền).
- Do đó, bạn sẽ **không tốn bất kỳ chi phí nào** khi thiết lập và thử nghiệm hệ thống đăng nhập này.

---

## 🛠️ Hướng Dẫn Chi Tiết Thiết Lập Cognito Trên AWS Console

Để game chạy được trên AWS, bạn cần làm theo 2 bước lớn sau:

### Bước 1: Tạo User Pool trên AWS Console (Thủ công hoặc qua CDK)
CDK Stack (`CognitoStack.cs` dưới đây) sẽ tự động tạo User Pool cho bạn khi deploy. Tuy nhiên, nếu bạn muốn tạo thủ công trên AWS Console để hiểu rõ hơn, hãy làm như sau:
1. Đăng nhập vào **AWS Management Console**.
2. Tìm kiếm dịch vụ **Cognito** và click **Create user pool**.
3. **Configure sign-in experience**:
   - Chọn **Username** và **Email**.
   - Bấm Next.
4. **Configure security requirements**:
   - Chọn **Cognito defaults** cho mật khẩu (hoặc cấu hình lại độ dài tối thiểu 8 ký tự).
   - Chọn **No MFA** (để tránh mất tiền gửi SMS và đơn giản hóa game).
   - Bấm Next.
5. **Configure sign-up experience**:
   - Tích chọn **Self-registration**.
   - Ở phần **Attribute verification**, chọn **Send email message, verify email** (Xác nhận qua email miễn phí).
   - Bấm Next.
6. **Configure message delivery**:
   - Chọn **Send email with Cognito** (Mặc định cho phép gửi tối thiểu 50 email/ngày miễn phí, đủ để dev. Khi chạy thật thì liên kết với Amazon SES).
   - Bấm Next.
7. **Integrate your app**:
   - Đặt tên cho User Pool (ví dụ: `GameUserPool`).
   - Ở phần **App client**, chọn **Public client** (Unity game client chạy trên máy người dùng, không thể giữ Client Secret an toàn).
   - Đặt tên cho App client (ví dụ: `GameMobileClient`).
   - Chọn **Don't generate a client secret**.
   - Trong phần **Allowed OAuth Flows**, đảm bảo có tích `ALLOW_USER_PASSWORD_AUTH` để sử dụng flow đăng nhập truyền thống.
   - Bấm Next và click **Create user pool**.
8. Lấy thông tin **User Pool ID** (ví dụ: `ap-southeast-1_xxxxxxxxx`) và **Client ID** (ví dụ: `7xxxxxxxxxxxxxxxxxxxxxxxxx`) hiển thị trên trang tổng quan để điền vào Unity.

---

## 📝 Các Thay Đổi Code Đề Xuất (Proposed Changes)

Dưới đây là các file sẽ được tạo mới và chỉnh sửa trong Giai Đoạn 4.

### 1. Infrastructure CDK (`infrastructure/`)

#### [NEW] [CognitoStack.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/infrastructure/src/Infrastructure/Stacks/CognitoStack.cs)
Tạo User Pool và App Client (không Client Secret).
```csharp
using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Constructs;

namespace Infrastructure.Stacks
{
    public class CognitoStack : Stack
    {
        public UserPool UserPool { get; }
        public UserPoolClient UserPoolClient { get; }

        public CognitoStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            UserPool = new UserPool(this, "GameUserPool", new UserPoolProps
            {
                UserPoolName = "RPG-Game-User-Pool",
                SelfSignUpEnabled = true,
                AutoVerify = new AutoVerifiedAttrs { Email = true },
                PasswordPolicy = new PasswordPolicy
                {
                    MinLength = 8,
                    RequireDigits = true,
                    RequireLowercase = true,
                    RequireUppercase = false,
                    RequireSymbols = false
                },
                AccountRecovery = AccountRecovery.EMAIL_ONLY
            });

            UserPoolClient = UserPool.AddClient("GameMobileClient", new UserPoolClientOptions
            {
                UserPoolClientName = "GameMobileClient",
                AuthFlows = new AuthFlow { UserPassword = true },
                GenerateSecret = false // Mobile/Unity client không giữ client secret
            });

            new CfnOutput(this, "UserPoolId", new CfnOutputProps { Value = UserPool.UserPoolId });
            new CfnOutput(this, "UserPoolClientId", new CfnOutputProps { Value = UserPoolClient.UserPoolClientId });
        }
    }
}
```

#### [MODIFY] [LambdaStack.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/infrastructure/src/Infrastructure/Stacks/LambdaStack.cs)
- Thêm `ConfirmSignUpFunction` và `RefreshTokenFunction`.
- Truyền `COGNITO_USER_POOL_ID` và `COGNITO_CLIENT_ID` qua env vars.
- Cấp quyền DynamoDB và Cognito cho các Lambdas.

#### [MODIFY] [ApiStack.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/infrastructure/src/Infrastructure/Stacks/ApiStack.cs)
- Thêm **CognitoUserPoolsAuthorizer** cho API Gateway.
- Tích hợp tất cả các hàm Lambda vào các resource tương ứng.
- Đánh dấu các route character, story, battle, inventory là **Protected** (yêu cầu Authorization Header với Cognito Access Token).

#### [MODIFY] [Program.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/infrastructure/src/Infrastructure/Program.cs)
- Khởi tạo `CognitoStack`.
- Cập nhật luồng truyền dependency giữa các Stack.

---

### 2. Backend Logic (`backend/`)

#### [MODIFY] [IAuthService.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Core/Services/Interfaces/IAuthService.cs)
Thêm các định nghĩa method cho Cognito:
```csharp
Task ConfirmSignUpAsync(string username, string confirmationCode);
Task<LoginResponse> RefreshTokenAsync(string refreshToken);
```

#### [MODIFY] [AuthService.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Core/Services/AuthService.cs)
Implement dummy methods để đảm bảo Plan A local mock compile thành công.

#### [NEW] [ConfirmSignUpHandler.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Handlers/Handlers/Auth/ConfirmSignUpHandler.cs)
Handler xử lý route `POST /auth/confirm`.

#### [NEW] [RefreshTokenHandler.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Handlers/Handlers/Auth/RefreshTokenHandler.cs)
Handler xử lý route `POST /auth/refresh`.

#### [MODIFY] [ServiceProviderBuilder.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Handlers/DependencyInjection/ServiceProviderBuilder.cs)
- Đăng ký `AmazonCognitoIdentityProviderClient` làm Singleton.
- Tự động swap `IAuthService` thành `CognitoAuthService` nếu phát hiện có env `COGNITO_USER_POOL_ID`.

---

### 3. Unity Client (`Assets/`)

#### [MODIFY] [GameConfigSO.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Script/Config/GameConfigSO.cs)
Thêm các trường cấu hình Cognito:
```csharp
[Header("AWS Cognito Settings (Plan B)")]
public string awsCognitoUserPoolId;
public string awsCognitoClientId;
public string awsCognitoRegion = "ap-southeast-1";
```

#### [NEW] [ConfirmSignUpController.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Script/Auth/UI/ConfirmSignUpController.cs)
Tạo UI controller cho màn hình/panel nhập mã OTP xác nhận tài khoản.

---

## 🔍 Kế Hoạch Xác Minh (Verification Plan)

### 1. Đăng ký & Đăng nhập thật trên AWS:
- Chuyển `GameConfigSO.useMockMode = false` trên Unity.
- Điền đầy đủ thông tin API Gateway URL, Cognito User Pool ID, Cognito Client ID.
- Mở màn hình đăng ký, điền email thực.
- Kiểm tra email để nhận mã OTP 6 chữ số.
- Nhập mã OTP vào panel xác nhận -> Đảm bảo tài khoản được verify thành công.
- Tiến hành đăng nhập với tài khoản vừa xác nhận -> Đảm bảo vào được MainMenu và lấy được dữ liệu.

### 2. Kiểm tra Refresh Token:
- Chờ token hết hạn (hoặc giả lập hết hạn) để xem Client có tự động gọi `/auth/refresh` và lấy Token mới mà không bắt người chơi đăng nhập lại hay không.
