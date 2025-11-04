package Randomnumberlambda.services;

import io.micronaut.context.annotation.*;
import jakarta.inject.Singleton;
import java.util.concurrent.ThreadLocalRandom;

@Singleton
public class AccountService {

    /**
     * Generate a random integer strictly greater than 1000 and strictly less than 3600.
     * This returns a value in the range [1001, 3599].
     */
    public int generateRandomNumber() {
        return ThreadLocalRandom.current().nextInt(1001, 3600);
    }
}
