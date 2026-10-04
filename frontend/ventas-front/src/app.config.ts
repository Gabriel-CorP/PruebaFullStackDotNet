import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withEnabledBlockingInitialNavigation, withInMemoryScrolling } from '@angular/router';
import Aura from '@primeng/themes/aura';
import { ConfirmationService, MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';
import { appRoutes } from './app.routes';
import { authInterceptor, errorInterceptor } from './app/core/interceptors/http.interceptors';
import { definePreset } from '@primeng/themes';

const VentasPreset = definePreset(Aura, {
    semantic: {
        primary: {
            50: '{red.50}',   100: '{red.100}', 200: '{red.200}', 300: '{red.300}',
            400: '{red.400}', 500: '{red.500}', 600: '{red.600}', 700: '{red.700}',
            800: '{red.800}', 900: '{red.900}', 950: '{red.950}'
        }
    }
});

export const appConfig: ApplicationConfig = {
    providers: [
        provideRouter(appRoutes, withInMemoryScrolling({ anchorScrolling: 'enabled', scrollPositionRestoration: 'enabled' }), withEnabledBlockingInitialNavigation()),
        provideHttpClient(withFetch(), withInterceptors([authInterceptor, errorInterceptor])),
        provideAnimationsAsync(),
        MessageService,
        ConfirmationService,
        providePrimeNG({
           theme: { preset: VentasPreset, options: { darkModeSelector: '.app-dark' } },
            translation: {
                firstDayOfWeek: 1,
                dayNames: ['domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado'],
                dayNamesShort: ['dom', 'lun', 'mar', 'mié', 'jue', 'vie', 'sáb'],
                dayNamesMin: ['D', 'L', 'M', 'X', 'J', 'V', 'S'],
                monthNames: ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'],
                monthNamesShort: ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'],
                today: 'Hoy',
                clear: 'Limpiar',
                weekHeader: 'Sm',
                accept: 'Sí',
                reject: 'No',
                apply: 'Aplicar',
                emptyMessage: 'Sin resultados',
                emptyFilterMessage: 'Sin resultados'
            }
        })
    ]
};
