import { Component, OnInit } from '@angular/core';
import { CompanyService } from '../../services/company.service';
import { CommonModule } from '@angular/common';
import { CompanyModalComponent } from '../company-modal/company-modal.component';

@Component({
  selector: 'app-company',
  standalone: true,
  imports: [CommonModule, CompanyModalComponent],
  templateUrl: './company.component.html',
  styleUrl: './company.component.css'
})
export class CompanyComponent implements OnInit {

  company:any = {};
  showModal = false;

  constructor(private service:CompanyService){}

  ngOnInit(){
    this.reload();
  }

  reload(){

    this.service.get()
      .subscribe((res:any)=>{

        if(res?.value)
          this.company = res.value;

      });

  }

  openModal(){
    this.showModal = true;
  }

}