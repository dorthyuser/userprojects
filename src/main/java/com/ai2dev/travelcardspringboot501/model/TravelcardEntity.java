package com.ai2dev.travelcardspringboot501.model;

import java.time.OffsetDateTime;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("travelcards")
public record TravelcardEntity(
        @Id @Column("id") Long id,
        @Column("travelcard_type") TravelcardTypeEnum travelcardType,
        @Column("travelcard_valid_from") OffsetDateTime travelcardValidFrom,
        @Column("travelcard_valid_to") OffsetDateTime travelcardValidTo,
        @Column("travelcard_name") String travelcardName,
        @Column("travelcard_number") String travelcardNumber,
        @Column("travelcard_requested_date") OffsetDateTime travelcardRequestedDate,
        @Column("travelcard_transaction_reference") String travelcardTransactionReference,
        @Column("travelcard_usable_to") OffsetDateTime travelcardUsableTo)
{
}
