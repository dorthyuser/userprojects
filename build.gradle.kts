plugins {
  java
  id("org.springframework.boot") version "3.1.12"
  id("io.spring.dependency-management") version "1.1.7"
  id("com.gradleup.shadow") version "8.3.9"
}
group = "com.ai2dev.sb_lambda_testng_2"
version = "0.0.1-SNAPSHOT"
java { toolchain { languageVersion.set(JavaLanguageVersion.of(21)) } }
repositories { mavenCentral() }
dependencies {
  implementation("org.springframework.boot:spring-boot-starter-web")
  implementation("org.springframework.boot:spring-boot-starter-validation")
  implementation("com.amazonaws.serverless:aws-serverless-java-container-springboot3:2.0.0")
  implementation("com.amazonaws:aws-lambda-java-core:1.2.3")
  implementation("com.amazonaws:aws-lambda-java-events:3.11.4")
  implementation("net.logstash.logback:logstash-logback-encoder:7.4") 
  runtimeOnly("ch.qos.logback:logback-classic")
  implementation("org.springframework.boot:spring-boot-starter-data-jdbc")
  implementation("com.zaxxer:HikariCP")
  runtimeOnly("org.postgresql:postgresql")
  // AWS Secrets Manager — credentials fetched at startup
  implementation(platform("software.amazon.awssdk:bom:2.25.0"))
  implementation("software.amazon.awssdk:secretsmanager")
  implementation("software.amazon.awssdk:sts")
  testImplementation("org.springframework.boot:spring-boot-starter-test")
}
tasks.getByName<org.springframework.boot.gradle.tasks.bundling.BootJar>("bootJar") {
  enabled = false
}
tasks.getByName<Jar>("jar") {
  enabled = true
}
tasks.shadowJar {
  archiveClassifier.set("")
  mergeServiceFiles()
  append("META-INF/spring/org.springframework.boot.autoconfigure.AutoConfiguration.imports")
  append("META-INF/spring.factories")
}
tasks.build { dependsOn(tasks.shadowJar) }
