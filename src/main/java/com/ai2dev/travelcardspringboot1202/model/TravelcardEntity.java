package com.ai2dev.travelcardspringboot1202.model;

import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("travelcards")
public record TravelcardEntity(
        @Id @Column("id") Long id,
        @Column("travelcard_type") TravelcardType travelcardType,
        @Column("travelcard_valid_from") java.time.OffsetDateTime travelcardValidFrom,
        @Column("travelcard_valid_to") java.time.OffsetDateTime travelcardValidTo,
        @Column("travelcard_name") String travelcardName,
        @Column("travelcard_number") String travelcardNumber,
        @Column("travelcard_requested_date") java.time.OffsetDateTime travelcardRequestedDate,
        @Column("travelcard_transaction_reference") String travelcardTransactionReference,
        @Column("travelcard_usable_to") java.time.OffsetDateTime travelcardUsableTo) {
}
