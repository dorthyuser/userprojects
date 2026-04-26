plugins {
  java
  id("org.springframework.boot") version "3.5.9"
  id("io.spring.dependency-management") version "1.1.7"
}
group = "com.ai2dev.zohoprojectsb"
version = "0.0.1-SNAPSHOT"
java { toolchain { languageVersion.set(JavaLanguageVersion.of(17)) } }
repositories { mavenCentral() }
dependencies {
  implementation("org.springframework.boot:spring-boot-starter-web")
  implementation("org.springframework.boot:spring-boot-starter-validation")
  runtimeOnly("ch.qos.logback:logback-classic")
  implementation("com.azure:azure-security-keyvault-secrets:4.7.0")
  implementation("com.azure:azure-identity:1.8.0")
   compileOnly("io.r2dbc:r2dbc-spi")          // ← ADD THIS
    compileOnly("io.r2dbc:r2dbc-pool")

  testImplementation("org.springframework.boot:spring-boot-starter-test")
}
