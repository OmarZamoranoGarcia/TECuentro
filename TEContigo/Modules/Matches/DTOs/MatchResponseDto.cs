namespace TEContigo.Modules.Matches.DTOs
{
    public class MatchResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? Id { get; set; }

        /// <summary>
        /// Se llena en RequestChatAsync cuando, tras esta llamada,
        /// ambos usuarios ya solicitaron el chat (listo para crear
        /// la futura Conversation). En los demás casos queda en null.
        /// </summary>
        public bool? BothUsersAccepted { get; set; }
    }
}
