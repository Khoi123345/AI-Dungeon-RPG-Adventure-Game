using System;

namespace GameShared.DTOs.Common
{
    /// <summary>
    /// Wrapper chuẩn hóa tất cả API response từ Lambda.
    /// Client luôn parse response theo cấu trúc này.
    /// </summary>
    [Serializable]
    public class ApiResponse<T>
    {
        public bool success;
        public string message;
        public T data;
        public string errorCode;
    }
}
