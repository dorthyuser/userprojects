package com.ai2dev.zohoprojectsb.model;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

public class UserRequest {
    @NotBlank
    private String first_name;

    @NotBlank
    private String last_name;

    @NotBlank
    @Email
    private String email;

    @NotNull
    private RoleRef role;

    @NotNull
    private ProfileRef profile;

    public UserRequest() {
    }

    public String getFirst_name() {
        return first_name;
    }

    public void setFirst_name(String first_name) {
        this.first_name = first_name;
    }

    public String getLast_name() {
        return last_name;
    }

    public void setLast_name(String last_name) {
        this.last_name = last_name;
    }

    public String getEmail() {
        return email;
    }

    public void setEmail(String email) {
        this.email = email;
    }

    public RoleRef getRole() {
        return role;
    }

    public void setRole(RoleRef role) {
        this.role = role;
    }

    public ProfileRef getProfile() {
        return profile;
    }

    public void setProfile(ProfileRef profile) {
        this.profile = profile;
    }

    public static class RoleRef {
        @NotBlank
        private String id;

        public RoleRef() {
        }

        public String getId() {
            return id;
        }

        public void setId(String id) {
            this.id = id;
        }
    }

    public static class ProfileRef {
        @NotBlank
        private String id;

        public ProfileRef() {
        }

        public String getId() {
            return id;
        }

        public void setId(String id) {
            this.id = id;
        }
    }
}
