plugins {
    id("io.micronaut.application") version "4.6.1"
    id("io.micronaut.library")
    id("com.gradleup.shadow") version "8.3.9"
}

micronaut {
    runtime("lambda_java")
    testRuntime("junit5")
    version("4.6.1")
    processing {
        incremental(true)
        annotations("com.ai2dev.testhello")
    }
}

dependencies {
    implementation("io.micronaut.data:micronaut-data-hibernate-jpa")
    implementation("io.micronaut.http:micronaut-http-client")
    implementation("io.micronaut.jackson:micronaut-jackson")
    implementation("jakarta.inject:jakarta.inject-api")
    testImplementation("io.micronaut.test:micronaut-test-junit5")
}

repositories {
    mavenCentral()
}