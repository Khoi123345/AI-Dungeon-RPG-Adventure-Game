# Hướng Dẫn Chi Tiết: Thiết Lập & Tích Hợp AWS Cognito Cho Unity Game

Để cấu hình AWS Cognito cho game, bạn có thể lựa chọn 1 trong 2 cách sau:
1. **Cách 1 (Khuyên dùng): Deploy tự động bằng AWS CDK** (Sử dụng các file code C# có sẵn trong thư mục `infrastructure/` để tự tạo tự động).
2. **Cách 2: Thiết lập thủ công trên giao diện Web của AWS Console**.

Dưới đây là hướng dẫn chi tiết từng bước cho cả hai cách và cách đưa thông tin vào Unity.

---

## 💡 Hiểu về AWS CDK vs AWS Web Console

- **AWS Web Console**: Là trang web của AWS (đăng nhập bằng trình duyệt) để bạn bấm click tạo tài nguyên thủ công.
- **AWS CDK (Cloud Development Kit)**: Là phương pháp **viết code để định nghĩa hạ tầng** (Infrastructure as Code). Thay vì bạn phải lên web bấm click thủ công hàng chục bước dễ nhầm lẫn, chúng ta viết code C# (trong file [CognitoStack.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/infrastructure/src/Infrastructure/Stacks/CognitoStack.cs), [ApiStack.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/infrastructure/src/Infrastructure/Stacks/ApiStack.cs)). Khi chạy lệnh `cdk deploy`, công cụ sẽ **tự động kết nối lên AWS và tạo chính xác** Cognito User Pool, API Gateway, Database giống y hệt thiết kế.

Để CDK có thể tự động tạo trên AWS, máy tính của bạn cần được cấp quyền truy cập thông qua **AWS CLI** (công cụ dòng lệnh xác thực tài khoản).

---

## 🛠️ Cách 1: Deploy Tự Động Bằng Code CDK (Nhanh & Chuẩn Xác)

Nếu bạn chọn cách này, bạn không cần phải tự cấu hình gì trên web cả, hệ thống sẽ tự sinh ra toàn bộ Cognito + Lambda + API Gateway và liên kết chúng lại với nhau.

### Bước 1.1: Tạo tài khoản IAM và lấy Access Key trên AWS Web
1. Đăng nhập vào [AWS Console](https://aws.amazon.com/console/).
2. Tìm dịch vụ **IAM** (Identity and Access Management).
3. Vào phần **Users** -> Click **Create user**.
4. Đặt tên (ví dụ: `game-developer`) -> Click **Next**.
5. Chọn **Attach policies directly**, tìm và tích chọn **AdministratorAccess** (quyền admin để tạo tài nguyên) -> Click **Next** -> Click **Create user**.
6. Chọn user vừa tạo -> Tab **Security credentials** -> Cuộn xuống phần **Access keys** -> Click **Create access key**.
7. Chọn mục **Command Line Interface (CLI)** -> Click **Create**.
8. **QUAN TRỌNG**: Tải file `.csv` chứa **Access Key ID** và **Secret Access Key** về máy. Bạn sẽ chỉ nhìn thấy nó một lần duy nhất này.

### Bước 1.2: Cấu hình CLI trên máy tính của bạn
1. Cài đặt [AWS CLI](https://aws.amazon.com/cli/) (nếu chưa có).
2. Mở Command Prompt hoặc PowerShell trên máy tính của bạn và gõ:
   ```bash
   aws configure
   ```
3. Điền các thông tin từ file `.csv` bạn tải ở Bước 1.1:
   - `AWS Access Key ID`: Dán Access Key của bạn.
   - `AWS Secret Access Key`: Dán Secret Key của bạn.
   - `Default region name`: Điền `ap-southeast-1` (Singapore, gần Việt Nam nhất và chi phí rẻ).
   - `Default output format`: Nhấn Enter (để mặc định).

### Bước 1.3: Chạy deploy
1. Mở Terminal tại thư mục: `d:\Unity\Project\AI-Dungeon-RPG-Adventure-Game\infrastructure`.
2. Chạy lệnh:
   ```bash
   cdk deploy --all
   ```
3. Chờ khoảng 3-5 phút. Khi hoàn tất, màn hình Terminal sẽ in ra các dòng **Outputs** màu xanh lá:
   - `GameCognitoStack.UserPoolId` (ví dụ: `ap-southeast-1_AbCdEf123`)
   - `GameCognitoStack.UserPoolClientId` (ví dụ: `7xyz9876543210`)
   - `GameApiStack.ApiUrl` (ví dụ: `https://xxxx.execute-api.ap-southeast-1.amazonaws.com/prod/`)
4. Lưu các thông tin này lại để điền vào Unity ở Bước 3.

---

## 🖥️ Cách 2: Thiết Lập Thủ Công Trên Web AWS Console

Nếu bạn không muốn dùng CDK để tự động tạo mà muốn tự tay click từng bước trên trình duyệt, hãy làm theo hướng dẫn dưới đây.

### Bước 2.1: Tạo User Pool trên Web
1. Đăng nhập vào [AWS Console](https://aws.amazon.com/console/).
2. Tìm dịch vụ **Cognito** ở thanh tìm kiếm và click vào nó.
3. Click nút **Create user pool**.
4. **Bước 1: Configure sign-in experience**:
   - Tích chọn **Username** và **Email** (để người chơi đăng nhập bằng username hoặc email).
   - Click **Next**.
5. **Bước 2: Configure security requirements**:
   - Chọn **Cognito defaults** cho mật khẩu.
   - Ở phần **Multi-factor authentication (MFA)**, chọn **No MFA** (Không dùng xác thực 2 lớp qua SMS/OTP để tránh bị tính phí gửi tin nhắn SMS).
   - Click **Next**.
6. **Bước 3: Configure sign-up experience**:
   - Giữ nguyên tích **Self-registration** (cho phép người chơi tự đăng ký tài khoản).
   - Cuộn xuống phần **Attribute verification**: Tích chọn **Send email message, verify email** (Xác nhận tài khoản bằng mã gửi về Email - **gói này miễn phí**).
   - Click **Next**.
7. **Bước 4: Configure message delivery**:
   - Ở mục **Email provider**, chọn **Send email with Cognito** (Đây là gói mặc định gửi tối đa 50 email xác thực/ngày miễn phí, phục vụ cho quá trình test game. Khi game phát hành chính thức bạn mới cần liên kết dịch vụ gửi mail chuyên nghiệp Amazon SES).
   - Click **Next**.
8. **Bước 5: Integrate your app**:
   - Đặt tên User Pool (ví dụ: `RPG-Game-User-Pool`).
   - Ở phần **App client**: Chọn loại **Public client** (vì Unity game client chạy trên máy người dùng, không thể bảo mật được Client Secret).
   - Đặt tên App client (ví dụ: `GameMobileClient`).
   - Chọn **Don't generate a client secret**.
   - Trong phần **Allowed OAuth Flows**, đảm bảo có tích chọn `ALLOW_USER_PASSWORD_AUTH` để game gọi API đăng nhập truyền thống.
   - Click **Next**.
9. **Bước 6: Review and create**:
   - Cuộn xuống dưới cùng và click **Create user pool**.
10. Lấy thông tin:
    - Click vào User Pool bạn vừa tạo -> copy **User Pool ID** ở đầu trang.
    - Chuyển sang tab **App integration** -> Cuộn xuống dưới cùng copy **Client ID**.

---

## 🎮 Bước 3: Điền Thông Tin Vào Unity Game

Dù bạn tạo Cognito bằng **Cách 1 (CDK)** hay **Cách 2 (Web)**, bạn đều sẽ nhận được 3 thông số:
1. `API Base URL` (Link API Gateway kết nối với backend).
2. `Cognito User Pool ID`.
3. `Cognito Client ID`.

Hãy thực hiện tích hợp vào Unity như sau:

1. Mở dự án Unity của bạn.
2. Tìm asset **GameConfig** trong mục **Project** (thường nằm ở `Assets/Script/Config/GameConfig.asset`).
3. Nhìn sang bảng **Inspector** bên phải và điền cấu hình:
   - **Api Base Url**: `https://xxxx.execute-api.ap-southeast-1.amazonaws.com/prod` (địa chỉ API Gateway của bạn).
   - **Use Mock Mode**: Bỏ tích (thiết lập về `false` để game chạy online thực tế thay vì dùng dữ liệu giả).
   - **Aws Cognito User Pool Id**: Dán `UserPoolId` của bạn vào.
   - **Aws Cognito Client Id**: Dán `UserPoolClientId` của bạn vào.
   - **Aws Cognito Region**: Điền `ap-southeast-1` (hoặc region bạn tạo Cognito).

4. Nhấn **Save** dự án và chạy game thử nghiệm! Khi đăng ký, game sẽ tự động gọi backend đẩy lên Cognito, gửi mã OTP về email của bạn. Bạn nhập mã OTP vào panel xác nhận để kích hoạt tài khoản và đăng nhập chơi game bình thường.
