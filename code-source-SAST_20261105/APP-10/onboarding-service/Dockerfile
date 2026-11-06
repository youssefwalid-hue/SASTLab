# APP-10 OnboardR — service d'onboarding KYC (Spring Boot). Image multi-stage.
FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /build
COPY pom.xml .
RUN mvn -q -B dependency:go-offline
COPY src ./src
RUN mvn -q -B clean package -DskipTests

FROM eclipse-temurin:21-jre
WORKDIR /app
COPY --from=build /build/target/onboardr-kyc.jar app.jar
EXPOSE 8080
ENV PORT=8080
# du connecteur (encodedSolidusHandling=decode). Le backend normalise alors /api/..%2fadmin
# vers /admin (la regle de filtrage du proxy, appliquee a la forme brute, est contournee).
ENTRYPOINT ["java", "-Dorg.apache.tomcat.util.buf.UDecoder.ALLOW_ENCODED_SLASH=true", "-jar", "app.jar"]
