package com.ai2dev.travelcardsb949.model;

import java.time.LocalDateTime;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("travelcards")
public record TravelcardEntity(@Id Long id, @Column("travelcard_type") TravelcardType travelcardType, @Column("travelcard_valid_from") LocalDateTime travelcardValidFrom, @Column("travelcard_valid_to") LocalDateTime travelcardValidTo, @Column("travelcard_name") String travelcardName, @Column("travelcard_number") String travelcardNumber, @Column("travelcard_requested_date") LocalDateTime travelcardRequestedDate, @Column("travelcard_transaction_reference") String travelcardTransactionReference, @Column("travelcard_usable_to") LocalDateTime travelcardUsableTo)
{
}