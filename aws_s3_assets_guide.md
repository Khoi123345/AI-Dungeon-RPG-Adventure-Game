# Báo Cáo & Hướng Dẫn: AWS S3 Assets cho AI Dungeon RPG

## Mục Lục
1. [Tại Sao Cần S3?](#1-tại-sao-cần-s3)
2. [Những Gì Đã Được Tạo Ra](#2-những-gì-đã-được-tạo-ra)
3. [Giải Thích Các Khái Niệm](#3-giải-thích-các-khái-niệm)
4. [Flow Hoạt Động Thực Tế](#4-flow-hoạt-động-thực-tế)
5. [Hướng Dẫn Deploy Theo Từng Trường Hợp](#5-hướng-dẫn-deploy-theo-từng-trường-hợp)
6. [Thông Tin Quan Trọng Cần Lưu](#6-thông-tin-quan-trọng-cần-lưu)

---

## 1. Tại Sao Cần S3?

### Vấn đề trước đây
Game có 3 loại file cần phục vụ cho người chơi:

| Loại file | Vị trí cũ | Vấn đề |
|-----------|-----------|--------|
| Hình ảnh (sprites, art) | Bundle trong Unity build | Build nặng, không cập nhật được |
| Âm thanh (BGM, SFX) | Bundle trong Unity build | File lớn (~25 MB), khó thay |
| Prompt AI (file `.md`) | Nằm trong code Lambda | Phải redeploy cả Lambda chỉ để sửa 1 câu prompt |

### Giải pháp: Lưu trên AWS S3 + CloudFront

```
Người chơi (VN) ──────► CloudFront CDN ──────► S3 Bucket
                         (Singapore Edge)        (lưu trữ thực tế)
                         Cache nhanh

Lambda Function ──────► S3 Bucket
(đọc prompt AI)          (đọc system_prompt.md, story_prompt.md)
```

**Lợi ích:**
- Cập nhật hình ảnh/âm thanh/prompt mà **không cần build lại Unity hay redeploy Lambda**
- CloudFront cache tại Singapore giảm latency cho user Việt Nam
- Chi phí rất thấp: ~$0.50 – $1.00/tháng giai đoạn dev

---

## 2. Những Gì Đã Được Tạo Ra

### Trên AWS Cloud (tự động qua CDK)

```
AWS Account : 569278273174
Region      : ap-southeast-1 (Singapore)
```

| Resource | Tên | Mô tả |
|----------|-----|-------|
| S3 Bucket | `game-assets-rpg-569278273174` | Kho lưu trữ assets |
| CloudFront CDN | `d3plikv8poewp8.cloudfront.net` | CDN phân phối đến người dùng |
| Cache Policy | `GameGraphicsCachePolicy` | Hình ảnh: cache 24 giờ |
| Cache Policy | `GameSoundsCachePolicy` | Âm thanh: cache 7 ngày |
| Cache Policy | `GamePromptsCachePolicy` | Prompt MD: cache 5 phút |
| Origin Access Control | `GameAssetsOAC` | Bảo mật: chỉ CloudFront được đọc S3 |

### Cấu trúc thư mục trong S3

```
s3://game-assets-rpg-569278273174/
├── graphics/
│   ├── sprites/
│   │   ├── Characters/   ← hình nhân vật
│   │   ├── Inventory/    ← hình vật phẩm, balo
│   │   ├── Weapon/       ← hình vũ khí, potion
│   │   └── GameUI/       ← hình UI game
│   └── art/              ← background art (JPG)
├── sounds/
│   ├── bgm/Ogg/          ← nhạc nền (Battle 1, Victory, Port Town...)
│   └── sfx/              ← hiệu ứng âm thanh (ButtonClick, Crash...)
└── prompts/
    ├── system_prompt.md  ← vai trò của AI (Game Master)
    ├── story_prompt.md   ← template tạo câu chuyện
    └── summary_prompt.md ← template tóm tắt câu chuyện
```

### Files code đã thay đổi trong project

| File | Loại thay đổi | Mục đích |
|------|--------------|---------|
| `infrastructure/Stacks/StorageStack.cs` | **Tạo mới** | CDK tạo S3 + CloudFront |
| `infrastructure/Stacks/LambdaStack.cs` | **Sửa** | Thêm env vars S3 cho Lambda |
| `infrastructure/Program.cs` | **Sửa** | Thêm StorageStack vào CDK App |
| `backend/AIStory/IPromptLoader.cs` | **Tạo mới** | Interface đọc prompt |
| `backend/AIStory/S3PromptLoader.cs` | **Tạo mới** | Đọc prompt từ S3, cache memory |
| `backend/AIStory/FileSystemPromptLoader.cs` | **Tạo mới** | Đọc prompt từ file (dev local) |
| `backend/AIStory/Builder/Impl/PromptBuilder.cs` | **Sửa** | Dùng IPromptLoader thay vì đọc file trực tiếp |
| `backend/DependencyInjection/ServiceProviderBuilder.cs` | **Sửa** | Đăng ký S3Client + chọn loader theo env var |
| `backend/GameBackend.Core.csproj` | **Sửa** | Thêm thư viện `AWSSDK.S3` |
| `tools/upload-assets.ps1` | **Tạo mới** | Script upload assets lên S3 |

---

## 3. Giải Thích Các Khái Niệm

### S3 Bucket là gì?
Giống như một **ổ cứng trên cloud**. Bạn để file vào đó, AWS giữ an toàn, và bạn truy cập qua HTTPS từ bất kỳ đâu.

### CloudFront CDN là gì?
**CDN = Content Delivery Network** — mạng lưới máy chủ phân tán toàn cầu.

```
Lần 1: User → CloudFront → S3    (X-Cache: Miss)  [lấy từ S3, lưu cache]
Lần 2: User → CloudFront          (X-Cache: Hit)   [lấy từ cache, cực nhanh]
```

Thay vì mỗi user đều phải kéo file từ S3, CloudFront giữ bản cache tại edge server gần họ nhất.

### CDK là gì?
Thay vì click tay trên AWS Console, CDK cho phép viết **code C#** để tạo infrastructure. Chạy `cdk deploy` → AWS tự tạo tất cả resources.

### S3PromptLoader hoạt động như thế nào?

```
Lambda cold start (lần đầu):
  → Đọc system_prompt.md từ S3
  → Lưu vào Dictionary trong memory (cache)

Lambda warm invoke (lần thứ 2, 3, ...):
  → Lấy từ memory cache ngay — KHÔNG gọi S3 thêm nữa
  → Container bị restart mới đọc S3 lại
```

**Kết quả**: Chỉ 1 S3 GET request per Lambda container lifetime. Tiết kiệm chi phí, giảm latency.

---

## 4. Flow Hoạt Động Thực Tế

### Khi người chơi chơi game (Story)

```
Unity Game Client
    │
    │  POST /story/action
    ▼
API Gateway → StoryActionFunction (Lambda)
                  │
                  │  [Lần đầu - cold start]
                  ├─► Đọc system_prompt.md từ S3
                  ├─► Cache vào memory
                  │
                  │  [Lần sau - warm]
                  ├─► Lấy prompt từ memory cache
                  │
                  └─► Gọi Amazon Bedrock AI
                          │
                          └─► Trả câu chuyện về Unity
```

### Khi muốn thay đổi prompt AI

```
Developer sửa system_prompt.md local
    │
    └─► aws s3 sync → file mới lên S3
                          │
        Lambda container cũ ──── dùng cache cũ (tối đa ~15 phút idle)
        Lambda container mới ──► đọc file mới từ S3
```

---

## 5. Hướng Dẫn Deploy Theo Từng Trường Hợp

> Chạy tất cả lệnh từ thư mục gốc:
> `D:\Unity\Project\AI-Dungeon-RPG-Adventure-Game\`

---

### CASE A: Thêm / Thay Hình Ảnh hoặc Âm Thanh

**Khi nào**: Thêm sprite mới, thay background, đổi nhạc BGM...

```powershell
# Bước 1: Đặt file vào đúng thư mục Unity
#   Sprites:  Assets\Graphics\Sprites\<thư mục>\
#   Art:      Assets\Graphics\Art\
#   BGM:      Assets\Sounds\BGM\Ogg\
#   SFX:      Assets\Sounds\<tên file>.wav

# Bước 2: Chạy script upload (tự động bỏ .meta files)
.\tools\upload-assets.ps1 -BucketName "game-assets-rpg-569278273174"

# Xong! Không cần làm gì thêm.
```

**Kiểm tra:**
```powershell
# Xem file đã lên chưa
aws s3 ls s3://game-assets-rpg-569278273174/graphics/ --recursive --region ap-southeast-1

# Test CDN URL
Invoke-WebRequest -Uri "https://d3plikv8poewp8.cloudfront.net/graphics/sprites/Weapon/TEN_FILE.png" -Method Head -UseBasicParsing | Select StatusCode
```

---

### CASE B: Sửa Prompt AI (KHÔNG cần redeploy Lambda!)

**Khi nào**: Thay đổi cách AI viết câu chuyện, điều chỉnh vai trò Game Master...

```powershell
# Bước 1: Mở và sửa file
#   backend\src\GameBackend.Core\AIStory\Content\Prompt\system_prompt.md
#   backend\src\GameBackend.Core\AIStory\Content\Prompt\story_prompt.md

# Bước 2: Upload chỉ thư mục prompts
aws s3 sync `
  "backend\src\GameBackend.Core\AIStory\Content\Prompt" `
  "s3://game-assets-rpg-569278273174/prompts/" `
  --cache-control "max-age=300" `
  --exclude "*.meta" `
  --region ap-southeast-1

# Xong! Lambda dùng prompt mới sau khi container restart tự nhiên.
```

---

### CASE C: Sửa Code Backend (Lambda C#)

**Khi nào**: Fix bug, thêm tính năng mới trong C# code...

```powershell
# Bước 1: Sửa code trong backend\src\

# Bước 2: Build và publish
dotnet publish `
  backend\src\GameBackend.Handlers\GameBackend.Handlers.csproj `
  --configuration Release `
  --runtime linux-x64 `
  --self-contained false `
  --output backend\src\GameBackend.Handlers\bin\Release\net8.0\publish

# Bước 3: Deploy Lambda
cd infrastructure
cdk deploy GameLambdaStack --require-approval never
cd ..

# Thời gian: ~2-3 phút
```

---

### CASE D: Thêm Lambda Function Mới + API Route

**Khi nào**: Thêm endpoint mới (/shop, /quest, /leaderboard...)

```powershell
# Bước 1: Tạo Handler class mới
#   backend\src\GameBackend.Handlers\Handlers\<Feature>\MyHandler.cs

# Bước 2: Khai báo function trong LambdaStack.cs
# Bước 3: Thêm route trong ApiStack.cs

# Bước 4: Build + Deploy 2 stack
dotnet publish backend\src\GameBackend.Handlers\... --configuration Release ...

cd infrastructure
cdk deploy GameLambdaStack GameApiStack --require-approval never
cd ..
```

---

### CASE E: Thay Đổi Infrastructure (S3, CloudFront, DynamoDB...)

**Khi nào**: Thay đổi cache TTL, thêm DynamoDB table, sửa S3 lifecycle...

```powershell
# Bước 1: Sửa file CDK
#   infrastructure\src\Infrastructure\Stacks\StorageStack.cs
#   infrastructure\src\Infrastructure\Stacks\DatabaseStack.cs
#   ... (stack nào liên quan)

# Bước 2: Xem preview thay đổi (khuyến nghị!)
cd infrastructure
cdk diff GameStorageStack

# Bước 3: Deploy
cdk deploy GameStorageStack --require-approval never
cd ..
```

---

### CASE F: Setup Từ Đầu (máy mới / sau khi xóa stack)

```powershell
# Bước 1: Build backend
dotnet publish `
  backend\src\GameBackend.Handlers\GameBackend.Handlers.csproj `
  --configuration Release --runtime linux-x64 --self-contained false `
  --output backend\src\GameBackend.Handlers\bin\Release\net8.0\publish

# Bước 2: Deploy tất cả stacks THEO THỨ TỰ
cd infrastructure
cdk deploy GameDatabaseStack  --require-approval never
cdk deploy GameCognitoStack   --require-approval never
cdk deploy GameStorageStack   --require-approval never
cdk deploy GameLambdaStack    --require-approval never
cdk deploy GameApiStack       --require-approval never
cdk deploy GameMonitoringStack --require-approval never
cd ..

# Bước 3: Upload assets
.\tools\upload-assets.ps1 -BucketName "game-assets-rpg-569278273174"

# Tổng thời gian: ~10-15 phút
```

> **Lưu ý thứ tự**: Phải deploy DatabaseStack và CognitoStack trước LambdaStack vì Lambda cần tên bảng DynamoDB và Cognito Pool ID.

---

## 6. Thông Tin Quan Trọng Cần Lưu

### Thông tin hạ tầng hiện tại

```
AWS Account ID  : 569278273174
Region          : ap-southeast-1 (Singapore)

S3 Bucket       : game-assets-rpg-569278273174
CloudFront URL  : https://d3plikv8poewp8.cloudfront.net

Lambda env vars (StartStoryFunction, StoryActionFunction):
  ASSETS_BUCKET_NAME = game-assets-rpg-569278273174
  ASSETS_CDN_URL     = https://d3plikv8poewp8.cloudfront.net
  PROMPT_SOURCE      = s3
```

### Cache TTL

| Loại | TTL | Nghĩa |
|------|-----|-------|
| Graphics | 24 giờ | Người dùng thấy hình mới sau 24h |
| Sounds | 7 ngày | Người dùng nghe nhạc mới sau 7 ngày |
| Prompts | 5 phút | CloudFront edge làm mới sau 5 phút |

### Kiểm tra hệ thống nhanh

```powershell
# S3 OK?
aws s3 ls s3://game-assets-rpg-569278273174/ --region ap-southeast-1

# CDN OK?
$r = Invoke-WebRequest -Uri "https://d3plikv8poewp8.cloudfront.net/prompts/system_prompt.md" -Method Head -UseBasicParsing
Write-Host "HTTP $($r.StatusCode) | Cache: $($r.Headers['X-Cache'])"
# Mong đợi: HTTP 200 | Cache: Hit from cloudfront

# Lambda env vars OK?
aws lambda get-function-configuration `
  --function-name StartStoryFunction `
  --region ap-southeast-1 `
  --query "Environment.Variables.{PROMPT_SOURCE:PROMPT_SOURCE,BUCKET:ASSETS_BUCKET_NAME}" `
  --output table
```

### Cấu trúc file quan trọng

```
project-root/
├── tools/
│   └── upload-assets.ps1              ← Script upload assets lên S3
│
├── infrastructure/src/Infrastructure/Stacks/
│   ├── StorageStack.cs                ← Định nghĩa S3 + CloudFront
│   ├── LambdaStack.cs                 ← Lambda + S3 permissions
│   └── Program.cs                     ← Thứ tự deploy stacks
│
├── backend/src/GameBackend.Core/AIStory/
│   ├── IPromptLoader.cs               ← Interface đọc prompt
│   ├── S3PromptLoader.cs              ← Đọc từ S3 + cache memory
│   ├── FileSystemPromptLoader.cs      ← Đọc từ file (local dev)
│   └── Content/Prompt/
│       ├── system_prompt.md           ← SUA FILE NAY de doi cach AI viet
│       ├── story_prompt.md            ← SUA FILE NAY de doi template cau chuyen
│       └── summary_prompt.md
│
└── Assets/ (Unity)
    ├── Graphics/Sprites/              ← Hinh anh game → upload len S3
    ├── Graphics/Art/                  ← Background art → upload len S3
    └── Sounds/                        ← Am thanh → upload len S3
```

---

> **Tóm lại 1 câu**: S3 là kho lưu trữ assets trên cloud, CloudFront là CDN phân phối nhanh đến người dùng, Lambda đọc prompt AI từ S3 thay vì từ file nội bộ — cho phép cập nhật mọi thứ mà không cần rebuild game hay redeploy toàn bộ hệ thống.
