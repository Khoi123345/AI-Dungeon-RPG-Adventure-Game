using System;

namespace GameShared.DTOs.Character
{
    [Serializable]
    public class CreateCharacterRequest
    {
        public string userId;
        public string name;
        public string className;
    }
}
