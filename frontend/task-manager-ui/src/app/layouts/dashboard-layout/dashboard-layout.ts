
import {
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import {
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';

import { AuthService } from '../../services/auth';
import { CurrentUser } from '../../models/auth';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatIconModule
  ],
  templateUrl: './dashboard-layout.html',
  styleUrl: './dashboard-layout.scss'
})
export class DashboardLayout implements OnInit {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly sidebarCollapsed = signal(false);
  readonly mobileMenuOpen = signal(false);
  readonly userMenuOpen = signal(false);

  readonly currentUser = signal<CurrentUser | null>(null);
  readonly userLoading = signal(true);
  readonly loggingOut = signal(false);

  readonly currentPath = signal(this.router.url.split('?')[0]);

  readonly currentPage = computed(() => {
    const path = this.currentPath();

    if (path === '/tasks') {
      return 'Mis tareas';
    }

    if (path === '/statistics') {
      return 'Estadísticas';
    }

    return 'Dashboard';
  });

  readonly userInitial = computed(() =>
    (this.currentUser()?.email?.charAt(0) ?? 'U').toUpperCase()
  );

  ngOnInit(): void {
    this.loadCurrentUser();

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd =>
          event instanceof NavigationEnd
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(event => {
        this.currentPath.set(
          event.urlAfterRedirects.split('?')[0]
        );

        this.mobileMenuOpen.set(false);
        this.userMenuOpen.set(false);
      });
  }

  loadCurrentUser(): void {
    this.userLoading.set(true);

    this.authService.getCurrentUser()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: user => {
          this.currentUser.set(user);
          this.userLoading.set(false);
        },
        error: err => {
          console.error('Error loading current user:', err);
          this.userLoading.set(false);
        }
      });
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(value => !value);
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update(value => !value);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }

  toggleUserMenu(): void {
    this.userMenuOpen.update(value => !value);
  }

  logout(): void {
    if (this.loggingOut()) {
      return;
    }

    this.loggingOut.set(true);
    this.userMenuOpen.set(false);

    this.authService.logout()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.loggingOut.set(false);
          void this.router.navigateByUrl('/login');
        },
        error: err => {
          console.error('Error logging out:', err);
          this.loggingOut.set(false);
          void this.router.navigateByUrl('/login');
        }
      });
  }
}
