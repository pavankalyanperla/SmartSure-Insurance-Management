import { Component, HostListener, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

interface Particle {
  id: number;
  left: string;
  top: string;
  size: string;
  dur: string;
  delay: string;
}

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent implements OnInit {
  scrolled = false;
  particles: Particle[] = [];

  ngOnInit(): void {
    this.particles = Array.from({ length: 18 }, (_, i) => ({
      id: i,
      left:  `${Math.random() * 100}%`,
      top:   `${Math.random() * 100}%`,
      size:  `${3 + Math.random() * 6}px`,
      dur:   `${8 + Math.random() * 12}s`,
      delay: `${Math.random() * 8}s`
    }));
  }

  @HostListener('window:scroll')
  onScroll(): void {
    this.scrolled = window.scrollY > 40;
  }
}
