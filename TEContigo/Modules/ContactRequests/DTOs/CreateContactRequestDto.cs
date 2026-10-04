namespace TEContigo.Modules.ContactRequests.DTOs
{
    public class CreateContactRequestDto
    {
        public long FoundItemId { get; set; }

        /// <summary>
        /// Opcional: si el contacto surge a partir de un Match ya existente
        /// (por ejemplo, el requester también tiene un LostItem y el
        /// algoritmo ya había detectado coincidencia), se puede vincular
        /// aquí para trazabilidad. La mayoría de las veces será null,
        /// que es justo el caso que esta tabla vino a resolver: alguien
        /// contacta sin tener ningún LostItem publicado.
        /// </summary>
        public long? MatchId { get; set; }
    }
}
