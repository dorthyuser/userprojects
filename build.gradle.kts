import com.github.jengelman.gradle.plugins.shadow.transformers.PropertiesFileTransformer

plugins {
    java
    id("org.springframework.boot") version "4.1.1"
    id("io.spring.dependency-management") version "1.1.7"
    id("com.gradleup.shadow") version "9.6.1"
}

group = "com.ai2dev"
version = "1.0.0"

java {
    toolchain {
        languageVersion = JavaLanguageVersion.of(17)
    }
}

repositories {
    mavenCentral()
}

dependencies {
    // Spring MVC without an embedded server: the Lambda container runs the requests
    implementation("org.springframework.boot:spring-boot-starter-webmvc") {
        exclude(group = "org.springframework.boot", module = "spring-boot-starter-tomcat")
    }
    implementation("org.springframework.boot:spring-boot-starter-restclient")
    implementation("com.amazonaws.serverless:aws-serverless-java-container-springboot4:3.0.2")
    // Added by the migration: libraries the migrated code needs
    implementation("software.amazon.awssdk:secretsmanager:2.57.3")
    implementation("org.springframework.boot:spring-boot-starter-jdbc")
    implementation("org.postgresql:postgresql")
}

// AWS Lambda runs one flat jar (shadowJar) instead of Spring Boot's nested jar
tasks.bootJar {
    enabled = false
}

tasks.jar {
    archiveClassifier = "plain"
}

tasks.shadowJar {
    archiveClassifier = ""
    // Shadow keeps only the first copy of a file by default: the metadata files of every
    // library must all reach the transformers below to be merged
    duplicatesStrategy = DuplicatesStrategy.INCLUDE
    mergeServiceFiles()
    mergeServiceFiles("META-INF/spring")
    append("META-INF/spring.handlers")
    append("META-INF/spring.schemas")
    transform<PropertiesFileTransformer> {
        paths.addAll("META-INF/spring.factories", "META-INF/spring-autoconfigure-metadata.properties")
        mergeStrategy = PropertiesFileTransformer.MergeStrategy.Append
    }
    filesNotMatching(
        listOf(
            "META-INF/services/**", "META-INF/spring/**", "META-INF/spring.handlers", "META-INF/spring.schemas",
            "META-INF/spring.factories", "META-INF/spring-autoconfigure-metadata.properties", "META-INF/*.kotlin_module"
        )
    ) {
        duplicatesStrategy = DuplicatesStrategy.EXCLUDE
    }
}

tasks.build {
    dependsOn(tasks.shadowJar)
}
