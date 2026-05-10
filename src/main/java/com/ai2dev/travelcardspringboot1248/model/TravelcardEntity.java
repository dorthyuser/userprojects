package com.ai2dev.travelcardspringboot1248.model;

import java.time.Instant;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("travelcards")
public record TravelcardEntity(
        @Id @Column("id") Integer id,
        @Column("travelcard_type") TravelcardType travelcardType,
        @Column("travelcard_valid_from") Instant travelcardValidFrom,
        @Column("travelcard_valid_to") Instant travelcardValidTo,
        @Column("travelcard_name") String travelcardName,
        @Column("travelcard_number") String travelcardNumber,
        @Column("travelcard_requested_date") Instant travelcardRequestedDate,
        @Column("travelcard_transaction_reference") String travelcardTransactionReference,
        @Column("travelcard_usable_to") Instant travelcardUsableTo)
{
}