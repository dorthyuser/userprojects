package com.ai2dev.demo_travelcard_sb.model;

import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("cardholders")
public class Cardholder {
 @Id
 private Long id;

 private Long travelcardId;

 private String cardholderTitle;

 private String cardholderForename;

 private String cardholderSurname;

 private CardholderType cardholderType;

 private String cardholderPhotoName;

 private String cardholderPhotoRrsKey;

 private String cardholderPhotoUrl;

 private String cardholderPhotoKey;

 public Long getId() {
 return id;
 }

 public void setId(Long id) {
 this.id = id;
 }

 @Column("travelcard_id")
 public Long getTravelcardId() {
 return travelcardId;
 }

 public void setTravelcardId(Long travelcardId) {
 this.travelcardId = travelcardId;
 }

 @Column("cardholder_title")
 public String getCardholderTitle() {
 return cardholderTitle;
 }

 public void setCardholderTitle(String cardholderTitle) {
 this.cardholderTitle = cardholderTitle;
 }

 @Column("cardholder_forename")
 public String getCardholderForename() {
 return cardholderForename;
 }

 public void setCardholderForename(String cardholderForename) {
 this.cardholderForename = cardholderForename;
 }

 @Column("cardholder_surname")
 public String getCardholderSurname() {
 return cardholderSurname;
 }

 public void setCardholderSurname(String cardholderSurname) {
 this.cardholderSurname = cardholderSurname;
 }

 @Column("cardholder_type")
 public CardholderType getCardholderType() {
 return cardholderType;
 }

 public void setCardholderType(CardholderType cardholderType) {
 this.cardholderType = cardholderType;
 }

 @Column("cardholder_photo_name")
 public String getCardholderPhotoName() {
 return cardholderPhotoName;
 }

 public void setCardholderPhotoName(String cardholderPhotoName) {
 this.cardholderPhotoName = cardholderPhotoName;
 }

 @Column("cardholder_photo_rrs_key")
 public String getCardholderPhotoRrsKey() {
 return cardholderPhotoRrsKey;
 }

 public void setCardholderPhotoRrsKey(String cardholderPhotoRrsKey) {
 this.cardholderPhotoRrsKey = cardholderPhotoRrsKey;
 }

 @Column("cardholder_photo_url")
 public String getCardholderPhotoUrl() {
 return cardholderPhotoUrl;
 }

 public void setCardholderPhotoUrl(String cardholderPhotoUrl) {
 this.cardholderPhotoUrl = cardholderPhotoUrl;
 }

 @Column("cardholder_photo_key")
 public String getCardholderPhotoKey() {
 return cardholderPhotoKey;
 }

 public void setCardholderPhotoKey(String cardholderPhotoKey) {
 this.cardholderPhotoKey = cardholderPhotoKey;
 }
}
