plugins {
  id("io.micronaut.application") version "4.3.6"
  id("com.gradleup.shadow") version "8.3.9"
}

micronaut {
  version("4.3.2")
  runtime("lambda_java")
  processing { incremental(true); annotations("com.ai2dev.devninja_ai2dev_lambda.**") }
}

java { toolchain { languageVersion.set(JavaLanguageVersion.of(17)) } }

repositories { mavenCentral() }

dependencies {
  implementation(platform("io.micronaut.platform:micronaut-platform:4.3.2"))
  implementation("io.micronaut.aws:micronaut-function-aws")

  implementation("io.micronaut:micronaut-jackson-databind")
  implementation("io.micronaut.validation:micronaut-validation")
  runtimeOnly("ch.qos.logback:logback-classic:1.5.6")
  annotationProcessor("io.micronaut:micronaut-inject-java")
  annotationProcessor("io.micronaut.validation:micronaut-validation-processor")
  annotationProcessor("io.micronaut.data:micronaut-data-processor")
  annotationProcessor("io.micronaut.data:micronaut-data-jdbc")
  implementation("io.micronaut.data:micronaut-data-jdbc")
  implementation("io.micronaut.sql:micronaut-jdbc-hikari")
  implementation("org.postgresql:postgresql")

}

application { mainClass.set("com.ai2dev.devninja_ai2dev_lambda.Application") }
