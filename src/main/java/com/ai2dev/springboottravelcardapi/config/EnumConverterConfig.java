package com.ai2dev.springboottravelcardapi.config;

import com.ai2dev.springboottravelcardapi.model.CardholderType;
import com.ai2dev.springboottravelcardapi.model.TravelcardType;
import java.sql.JDBCType;
import java.util.Arrays;
import org.postgresql.util.PGobject;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.convert.converter.Converter;
import org.springframework.data.convert.WritingConverter;
import org.springframework.data.jdbc.core.convert.JdbcCustomConversions;
import org.springframework.data.jdbc.core.mapping.JdbcValue;

@Configuration
public class EnumConverterConfig
{
    @Bean
    public JdbcCustomConversions jdbcCustomConversions()
    {
        return new JdbcCustomConversions(Arrays.asList(
                new TravelcardTypeConverter(),
                new CardholderTypeConverter()));
    }

    @WritingConverter
    public static class TravelcardTypeConverter implements Converter<TravelcardType, JdbcValue>
    {
        @Override
        public JdbcValue convert(TravelcardType source)
        {
            try
            {
                PGobject object = new PGobject();
                object.setType("travelcard_type_enum");
                object.setValue(source.name());
                return JdbcValue.of(object, JDBCType.OTHER);
            }
            catch (Exception ex)
            {
                throw new IllegalArgumentException("Unable to convert travelcard type");
            }
        }
    }

    @WritingConverter
    public static class CardholderTypeConverter implements Converter<CardholderType, JdbcValue>
    {
        @Override
        public JdbcValue convert(CardholderType source)
        {
            try
            {
                PGobject object = new PGobject();
                object.setType("cardholder_type_enum");
                object.setValue(source.name());
                return JdbcValue.of(object, JDBCType.OTHER);
            }
            catch (Exception ex)
            {
                throw new IllegalArgumentException("Unable to convert cardholder type");
            }
        }
    }
}
