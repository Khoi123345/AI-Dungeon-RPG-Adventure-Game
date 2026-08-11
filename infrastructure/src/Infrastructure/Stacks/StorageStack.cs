using Amazon.CDK;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace Infrastructure.Stacks
{
    /// <summary>
    /// StorageStack: S3 Bucket chứa game assets (hình ảnh, âm thanh, prompt markdown)
    /// + CloudFront CDN để giảm latency và chi phí transfer cho client ở Việt Nam.
    ///
    /// Chi phí ước tính: ~$0.50-$1.00/tháng ở giai đoạn dev/test (< 50 MB assets).
    /// </summary>
    public class StorageStack : Stack
    {
        /// <summary>
        /// Tên S3 Bucket — export cho LambdaStack để grant read permission.
        /// </summary>
        public Bucket AssetsBucket { get; }

        /// <summary>
        /// CloudFront distribution URL — client Unity dùng URL này để tải assets qua HTTPS.
        /// </summary>
        public string CloudFrontUrl { get; }

        public StorageStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            // --- S3 Bucket ---
            // BlockPublicAccess.BLOCK_ALL: không public trực tiếp, chỉ cho phép qua CloudFront OAC
            AssetsBucket = new Bucket(this, "GameAssetsBucket", new BucketProps
            {
                BucketName = $"game-assets-rpg-{this.Account}",
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                Versioned = true,              // cho phép rollback asset khi cần
                RemovalPolicy = RemovalPolicy.RETAIN, // giữ bucket khi xóa stack
                AutoDeleteObjects = false,

                // CORS: cho phép Unity client hoặc browser đọc asset trực tiếp (fallback)
                Cors = new[]
                {
                    new CorsRule
                    {
                        AllowedMethods = new[] { HttpMethods.GET, HttpMethods.HEAD },
                        AllowedOrigins = new[] { "*" },
                        AllowedHeaders = new[] { "*" },
                        MaxAge = 3000
                    }
                },

                // Lifecycle rules — tối ưu chi phí tự động
                LifecycleRules = new[]
                {
                    // Hình ảnh: chuyển sang Intelligent-Tiering sau 30 ngày (tự động giảm tier)
                    new LifecycleRule
                    {
                        Id = "graphics-intelligent-tiering",
                        Prefix = "graphics/",
                        Enabled = true,
                        Transitions = new[]
                        {
                            new Transition
                            {
                                StorageClass = StorageClass.INTELLIGENT_TIERING,
                                TransitionAfter = Duration.Days(30)
                            }
                        }
                    },
                    // Âm thanh: Standard-IA sau 60 ngày (ít thay đổi, ít dùng)
                    new LifecycleRule
                    {
                        Id = "sounds-standard-ia",
                        Prefix = "sounds/",
                        Enabled = true,
                        Transitions = new[]
                        {
                            new Transition
                            {
                                StorageClass = StorageClass.INFREQUENT_ACCESS,
                                TransitionAfter = Duration.Days(60)
                            }
                        }
                    }
                    // Prompt MD: giữ S3 Standard (hot path, < 1 KB, không chuyển tier)
                }
            });

            // --- CloudFront Origin Access Control (OAC) ---
            // OAC thay thế OAI (deprecated) — chỉ CloudFront mới được phép đọc S3
            var oac = new CfnOriginAccessControl(this, "GameAssetsOAC", new CfnOriginAccessControlProps
            {
                OriginAccessControlConfig = new CfnOriginAccessControl.OriginAccessControlConfigProperty
                {
                    Name = "GameAssetsOAC",
                    OriginAccessControlOriginType = "s3",
                    SigningBehavior = "always",
                    SigningProtocol = "sigv4"
                }
            });

            // --- CloudFront Distribution ---
            var s3Origin = new S3Origin(AssetsBucket);

            var distribution = new Distribution(this, "GameAssetsDistribution", new DistributionProps
            {
                Comment = "CDN for AI Dungeon RPG game assets — ap-southeast-1",
                // PriceClass.PRICE_CLASS_100: US, EU, Asia Pacific (bao gồm SG/VN)
                // Rẻ hơn ALL, đủ dùng cho user Việt Nam
                PriceClass = PriceClass.PRICE_CLASS_100,

                DefaultBehavior = new BehaviorOptions
                {
                    Origin = s3Origin,
                    ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                    // Compress: tự bật gzip/brotli cho text (JSON, MD) → giảm transfer
                    Compress = true,
                    CachePolicy = CachePolicy.CACHING_OPTIMIZED,
                    AllowedMethods = AllowedMethods.ALLOW_GET_HEAD
                },

                // Cache behaviors riêng cho từng loại asset
                AdditionalBehaviors = new Dictionary<string, IBehaviorOptions>
                {
                    // Hình ảnh: cache 24 giờ — thay đổi ít, ổn định
                    ["/graphics/*"] = new BehaviorOptions
                    {
                        Origin = s3Origin,
                        ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                        Compress = true,
                        AllowedMethods = AllowedMethods.ALLOW_GET_HEAD,
                        CachePolicy = new CachePolicy(this, "GraphicsCachePolicy", new CachePolicyProps
                        {
                            CachePolicyName = "GameGraphicsCachePolicy",
                            Comment = "Cache sprites/art 24 hours",
                            DefaultTtl = Duration.Hours(24),
                            MaxTtl = Duration.Days(365),
                            MinTtl = Duration.Seconds(0),
                            EnableAcceptEncodingGzip = true
                        })
                    },
                    // Âm thanh: cache 7 ngày — BGM/SFX hầu như không thay đổi
                    ["/sounds/*"] = new BehaviorOptions
                    {
                        Origin = s3Origin,
                        ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                        Compress = false, // audio đã compressed sẵn (OGG/MP3)
                        AllowedMethods = AllowedMethods.ALLOW_GET_HEAD,
                        CachePolicy = new CachePolicy(this, "SoundsCachePolicy", new CachePolicyProps
                        {
                            CachePolicyName = "GameSoundsCachePolicy",
                            Comment = "Cache BGM/SFX 7 days",
                            DefaultTtl = Duration.Days(7),
                            MaxTtl = Duration.Days(365),
                            MinTtl = Duration.Seconds(0),
                            EnableAcceptEncodingGzip = false
                        })
                    },
                    // Prompt MD: cache ngắn 5 phút — cần cập nhật nhanh không redeploy Lambda
                    ["/prompts/*"] = new BehaviorOptions
                    {
                        Origin = s3Origin,
                        ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                        Compress = true,
                        AllowedMethods = AllowedMethods.ALLOW_GET_HEAD,
                        CachePolicy = new CachePolicy(this, "PromptsCachePolicy", new CachePolicyProps
                        {
                            CachePolicyName = "GamePromptsCachePolicy",
                            Comment = "Cache prompt MD files 5 minutes",
                            DefaultTtl = Duration.Minutes(5),
                            MaxTtl = Duration.Hours(1),
                            MinTtl = Duration.Seconds(0),
                            EnableAcceptEncodingGzip = true
                        })
                    }
                }
            });

            // Gắn OAC vào distribution (CloudFormation escape hatch vì CDK chưa support native)
            var cfnDistribution = distribution.Node.DefaultChild as CfnDistribution;
            cfnDistribution!.AddOverride(
                "Properties.DistributionConfig.Origins.0.S3OriginConfig.OriginAccessIdentity",
                ""
            );
            cfnDistribution.AddOverride(
                "Properties.DistributionConfig.Origins.0.OriginAccessControlId",
                oac.AttrId
            );

            // Grant CloudFront OAC quyền đọc S3
            AssetsBucket.AddToResourcePolicy(new Amazon.CDK.AWS.IAM.PolicyStatement(
                new Amazon.CDK.AWS.IAM.PolicyStatementProps
                {
                    Effect = Amazon.CDK.AWS.IAM.Effect.ALLOW,
                    Principals = new Amazon.CDK.AWS.IAM.IPrincipal[]
                    {
                        new Amazon.CDK.AWS.IAM.ServicePrincipal("cloudfront.amazonaws.com")
                    },
                    Actions = new[] { "s3:GetObject" },
                    Resources = new[] { AssetsBucket.ArnForObjects("*") },
                    Conditions = new Dictionary<string, object>
                    {
                        {
                            "StringEquals",
                            new Dictionary<string, string>
                            {
                                { "AWS:SourceArn", $"arn:aws:cloudfront::{this.Account}:distribution/{distribution.DistributionId}" }
                            }
                        }
                    }
                }
            ));

            CloudFrontUrl = $"https://{distribution.DistributionDomainName}";

            // --- Outputs ---
            new CfnOutput(this, "AssetsBucketName", new CfnOutputProps
            {
                Value = AssetsBucket.BucketName,
                Description = "S3 bucket name for game assets (graphics, sounds, prompts)"
            });

            new CfnOutput(this, "CloudFrontDomain", new CfnOutputProps
            {
                Value = distribution.DistributionDomainName,
                Description = "CloudFront CDN domain — dùng URL này trong Unity client và Lambda"
            });

            new CfnOutput(this, "CloudFrontUrl", new CfnOutputProps
            {
                Value = CloudFrontUrl,
                Description = "Full HTTPS CDN URL prefix"
            });
        }
    }
}
