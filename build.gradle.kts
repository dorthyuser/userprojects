plugins {
  java
  id("org.springframework.boot") version "3.5.9"
  id("io.spring.dependency-management") version "1.1.7"
}

group = "com.ai2dev.dashboard"
version = "0.0.1-SNAPSHOT"

java {
  toolchain {
    languageVersion.set(JavaLanguageVersion.of(17))
  }
}

repositories {
  mavenCentral()
}

dependencies {
  implementation("org.springframework.boot:spring-boot-starter-web")
  implementation("org.springframework.boot:spring-boot-starter-validation")
  implementation("org.springframework.boot:spring-boot-starter-data-jdbc")
  implementation("com.zaxxer:HikariCP")
  runtimeOnly("com.mysql:mysql-connector-j")
  runtimeOnly("ch.qos.logback:logback-classic")
  compileOnly("io.r2dbc:r2dbc-spi")
  compileOnly("io.r2dbc:r2dbc-pool")
  testImplementation("org.springframework.boot:spring-boot-starter-test")
}
