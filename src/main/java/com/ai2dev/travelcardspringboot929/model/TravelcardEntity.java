package com.ai2dev.travelcardspringboot929.model;

import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

import java.time.ZonedDateTime;

@Table("travelcards")
public record TravelcardEntity(
        @Id @Column("id") Integer id,
        @Column("travelcard_type") TravelcardType travelcardType,
        @Column("travelcard_valid_from") ZonedDateTime travelcardValidFrom,
        @Column("travelcard_valid_to") ZonedDateTime travelcardValidTo,
        @Column("travelcard_name") String travelcardName,
        @Column("travelcard_number") String travelcardNumber,
        @Column("travelcard_requested_date") ZonedDateTime travelcardRequestedDate,
        @Column("travelcard_transaction_reference") String travelcardTransactionReference,
        @Column("travelcard_usable_to") ZonedDateTime travelcardUsableTo) {
}
