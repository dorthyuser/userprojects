package com.ai2dev.travelcardspringboot501.model;

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
        @Column("cardholder_type") CardholderTypeEnum cardholderType,
        @Column("cardholder_photo_name") String cardholderPhotoName,
        @Column("cardholder_photo_rrs_key") String cardholderPhotoRrsKey,
        @Column("cardholder_photo_url") String cardholderPhotoUrl,
        @Column("cardholder_photo_key") String cardholderPhotoKey)
{
}
