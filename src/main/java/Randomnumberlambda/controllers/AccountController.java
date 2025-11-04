package Randomnumberlambda.controllers;

import io.micronaut.http.annotation.Controller;
import io.micronaut.http.annotation.Get;
import jakarta.inject.Inject;
import java.util.Map;
import Randomnumberlambda.services.AccountService;

@Controller("/account")
public class AccountController {

    private final AccountService accountService;

    @Inject
    public AccountController(AccountService accountService) {
        this.accountService = accountService;
    }

    @Get("/random")
    public Map<String, Object> random() {
        int number = accountService.generateRandomNumber();
        return Map.of("randomNumber", number);
    }
}
