// Every version is in gradle/libs.versions.toml (the version catalog: "libs")
plugins {
    java
    alias(libs.plugins.spring.boot)
    alias(libs.plugins.spring.dependency.management)
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

// gradle.lockfile pins every library the build resolves (also the ones a library brings in): every build
// gets the versions the project was built and checked with
dependencyLocking {
    lockAllConfigurations()
}

dependencies {
    // Spring MVC without an embedded server: the Lambda container runs the requests
    implementation(libs.spring.boot.starter.webmvc) {
        exclude(group = "org.springframework.boot", module = "spring-boot-starter-tomcat")
    }
    implementation(libs.spring.boot.starter.restclient)
    implementation(libs.aws.serverless.java.container.springboot4)
    // Lambda SnapStart's runtime hooks (config/SnapStartHooks.java)
    implementation(libs.crac)
    // Added by the migration: libraries the migrated code needs
    implementation(libs.spring.boot.starter.jdbc)
    implementation(libs.postgresql)
    implementation(libs.secretsmanager)
}

// AWS Lambda runs a .zip (AWS's packaging for Java functions): the project's own jar and every runtime
// library in lib/, each jar as it is — no Spring Boot nested jar, no merged (shaded) jar
tasks.bootJar {
    enabled = false
}

tasks.register<Zip>("buildZip") {
    into("lib") {
        from(tasks.jar)
        from(configurations.runtimeClasspath)
    }
}

tasks.build {
    dependsOn("buildZip")
}
