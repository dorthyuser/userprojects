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
    // A local run (./gradlew bootRun): an embedded server, only on bootRun's classpath — never in the function's .zip.
    // Spring Boot's Tomcat module, not its starter: with the starter's excludes, Gradle drops spring-web from bootRun
    developmentOnly(libs.spring.boot.tomcat)
    // The tests (src/test/java): JUnit 5 with Spring Boot's test libraries
    testImplementation(libs.spring.boot.starter.test)
    testRuntimeOnly(libs.junit.platform.launcher)
    // Added by the migration: libraries the migrated code needs
    implementation(libs.secretsmanager)
    implementation(libs.spring.boot.starter.jdbc)
    implementation(libs.postgresql)
}

// Releases with a fix for a known high or critical vulnerability, pinned by the build's security scan (also of a
// library another one brings in); their versions are in gradle/libs.versions.toml
dependencyManagement {
    dependencies {
        // Pinned by the security scan
    }
}

// AWS Lambda runs a .zip (AWS's packaging for Java functions): the project's own jar and every runtime
// library in lib/, each jar as it is — no Spring Boot nested jar, no merged (shaded) jar. The production
// runtime classpath, as Spring Boot packages it: without the developmentOnly libraries (the local run's server)
tasks.bootJar {
    enabled = false
}

tasks.register<Zip>("buildZip") {
    into("lib") {
        from(tasks.jar)
        from(configurations.productionRuntimeClasspath)
    }
}

tasks.build {
    dependsOn("buildZip")
}

// A local run: ./gradlew bootRun starts the "local" profile (application-local.properties) on http://localhost:8080;
// with a database, docker compose up -d first (compose.yaml)
tasks.bootRun {
    systemProperty("spring.profiles.active", "local")
}

// The tests run with every build; a failure shows its message (what is wrong) in the build output
tasks.test {
    useJUnitPlatform()
    testLogging {
        events("failed")
        exceptionFormat = org.gradle.api.tasks.testing.logging.TestExceptionFormat.FULL
    }
}
