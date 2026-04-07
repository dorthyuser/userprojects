package com.ai2dev.demo_travelcard_sb.dto;

import com.ai2dev.demo_travelcard_sb.model.CardholderType;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

public record CardholderDto(
 @NotBlank
 @Size(min = 1, max = 15)
 String cardholderTitle,

 @NotBlank
 @Size(min = 1, max = 100)
 String cardholderForename,

 @NotBlank
 @Size(min = 1, max = 100)
 String cardholderSurname,

 @NotNull
 CardholderType cardholderType,

 @NotBlank
 @Size(min = 1, max = 100)
 String cardholderPhotoName,

 @Size(min = 39, max = 42)
 String cardholderPhotoRrsKey,

 @Size(min = 20, max = 2048)
 @Pattern(regexp = "^(https?://)[A-Za-z0-9._~:/?#@!$&'()*+,;=%-]+$")
 String cardholderPhotoUrl,

 @Size(min = 39, max = 42)
 String cardholderPhotoKey
) {}
