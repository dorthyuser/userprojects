plugins {
  java
  id("org.springframework.boot") version "4.0.3"
  id("io.spring.dependency-management") version "1.1.7"
}

group = "com.ai2dev.springbootaetest620"
version = "0.0.1-SNAPSHOT"

java {
  toolchain {
    languageVersion.set(JavaLanguageVersion.of(21))
  }
}

repositories {
  mavenCentral()
}

dependencies {
  implementation("org.springframework.boot:spring-boot-starter-web")
  implementation("org.springframework.boot:spring-boot-starter-validation")
  runtimeOnly("ch.qos.logback:logback-classic")
  implementation("net.logstash.logback:logstash-logback-encoder:7.4")

  implementation("com.azure:azure-security-keyvault-secrets:4.8.0")
  implementation("com.azure:azure-identity:1.11.0")
  implementation("org.springframework.boot:spring-boot-starter-data-jdbc")
  implementation("com.zaxxer:HikariCP")
  implementation("org.postgresql:postgresql")
  testImplementation("org.springframework.boot:spring-boot-starter-test")
}
