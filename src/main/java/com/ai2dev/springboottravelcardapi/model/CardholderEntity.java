package com.ai2dev.springboottravelcardapi.model;

import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("cardholders")
public record CardholderEntity(
        @Id @Column("id") Long id,
        @Column("travelcard_id") Long travelcardId,
        @Column("cardholder_title") String cardholderTitle,
        @Column("cardholder_forename") String cardholderForename,
        @Column("cardholder_surname") String cardholderSurname,
        @Column("cardholder_type") CardholderType cardholderType,
        @Column("cardholder_photo_name") String cardholderPhotoName,
        @Column("cardholder_photo_rrs_key") String cardholderPhotoRRSKey,
        @Column("cardholder_photo_url") String cardholderPhotoURL,
        @Column("cardholder_photo_key") String cardholderPhotoKey)
{
}
