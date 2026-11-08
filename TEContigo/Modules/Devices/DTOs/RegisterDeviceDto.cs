namespace TEContigo.Modules.Devices.DTOs
{
    public class RegisterDeviceDto
    {
        /// <summary>
        /// Token de Expo, con la forma ExponentPushToken[xxxx].
        /// </summary>
        public string PushToken { get; set; } = string.Empty;

        /// <summary>
        /// "ios" o "android".
        /// </summary>
        public string Platform { get; set; } = string.Empty;
    }
}
