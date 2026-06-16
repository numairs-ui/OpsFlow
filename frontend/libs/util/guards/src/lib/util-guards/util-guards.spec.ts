import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UtilGuards } from './util-guards';

describe('UtilGuards', () => {
  let component: UtilGuards;
  let fixture: ComponentFixture<UtilGuards>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UtilGuards],
    }).compileComponents();

    fixture = TestBed.createComponent(UtilGuards);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
