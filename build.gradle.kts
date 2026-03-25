plugins {
  java
  id("org.springframework.boot") version "3.5.9"
  id("io.spring.dependency-management") version "1.1.7"
  id("com.gradleup.shadow") version "8.3.9"
}
group = "com.ai2dev.test_sb_java_travelcard"
version = "0.0.1-SNAPSHOT"
java { toolchain { languageVersion.set(JavaLanguageVersion.of(17)) } }
repositories { mavenCentral() }
dependencies {
  implementation("org.springframework.boot:spring-boot-starter-web")
  implementation("org.springframework.boot:spring-boot-starter-validation")
  implementation("com.amazonaws.serverless:aws-serverless-java-container-springboot3:2.0.0")
  implementation("com.amazonaws:aws-lambda-java-core:1.2.3")
  implementation("com.amazonaws:aws-lambda-java-events:3.11.4")
  runtimeOnly("ch.qos.logback:logback-classic")
  implementation("org.springframework.boot:spring-boot-starter-data-jdbc")
  implementation("com.zaxxer:HikariCP")
  runtimeOnly("org.postgresql:postgresql")
  testImplementation("org.springframework.boot:spring-boot-starter-test")
}
tasks.shadowJar {
  archiveClassifier.set("")
  mergeServiceFiles()
}
tasks.build { dependsOn(tasks.shadowJar) }
